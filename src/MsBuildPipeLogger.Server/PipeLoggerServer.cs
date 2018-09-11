using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// Receives MSBuild logging events over a pipe. This is the base class for <see cref="AnonymousPipeLoggerServer"/>
    /// and <see cref="NamedPipeLoggerServer"/>.
    /// </summary>
    public class PipeLoggerServer : EventArgsDispatcher, IDisposable
    {
        // This comes from https://github.com/KirillOsenkov/MSBuildStructuredLog/blob/master/src/StructuredLogger/BinaryLogger/BinaryLogger.cs
        // It should match the version of the files that were copied into MsBuildPipeLogger.Logger from MSBuildStructuredLog
        private const int FileFormatVersion = 7;

        private readonly BinaryReader _binaryReader;
        private readonly Func<BuildEventArgs> _read;
        
        /// <summary>
        /// Creates a server that receives MSBuild events over a specified pipe.
        /// </summary>
        /// <param name="pipeStream">The pipe to receive events from.</param>
        public PipeLoggerServer(PipeStream pipeStream)
        {
            PipeStream = pipeStream;
            _binaryReader = new BinaryReader(PipeStream);

            // Use reflection to get the Microsoft.Build.Logging.BuildEventArgsReader.Read() method
            object argsReader;
            Type buildEventArgsReader = typeof(BinaryLogger).GetTypeInfo().Assembly.GetType("Microsoft.Build.Logging.BuildEventArgsReader");
            ConstructorInfo readerCtor = buildEventArgsReader.GetConstructor(new[] { typeof(BinaryReader) });
            if(readerCtor != null)
            {
                argsReader = readerCtor.Invoke(new[] { _binaryReader });
            }
            else
            {
                readerCtor = buildEventArgsReader.GetConstructor(new[] { typeof(BinaryReader), typeof(int) });
                argsReader = readerCtor.Invoke(new object[] { _binaryReader, 7 });
            }
            MethodInfo readMethod = buildEventArgsReader.GetMethod("Read");
            _read = (Func<BuildEventArgs>)readMethod.CreateDelegate(typeof(Func<BuildEventArgs>), argsReader);
        }

        protected PipeStream PipeStream { get; }

        /// <summary>
        /// Reads a single event from the pipe. This method blocks until an event is received,
        /// there are no more events, or the pipe is closed.
        /// </summary>
        /// <returns><c>true</c> if an event was read, <c>false</c> otherwise.</returns>
        public virtual bool Read()
        {
            // Now read one message from the stream
            try
            {
                BuildEventArgs args = _read();
                if (args != null)
                {
                    Dispatch(args);
                    return true;
                }
            }
            catch(EndOfStreamException)
            {
                // Nothing else to read
            }
            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _binaryReader.Dispose();
            PipeStream.Dispose();
        }
    }
}
