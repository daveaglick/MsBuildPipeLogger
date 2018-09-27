using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// Receives MSBuild logging events over a pipe. This is the base class for <see cref="AnonymousPipeLoggerServer"/>
    /// and <see cref="NamedPipeLoggerServer"/>.
    /// </summary>
    public class PipeLoggerServer : EventArgsDispatcher, IDisposable
    {
        private readonly BinaryReader _binaryReader;
        private readonly BuildEventArgsReaderProxy _buildEventArgsReader;
        
        internal PipeBuffer Buffer { get; } = new PipeBuffer();
        
        protected PipeStream PipeStream { get; }

        protected CancellationToken CancellationToken { get; }

        /// <summary>
        /// Creates a server that receives MSBuild events over a specified pipe.
        /// </summary>
        /// <param name="pipeStream">The pipe to receive events from.</param>
        public PipeLoggerServer(PipeStream pipeStream)
            : this(pipeStream, CancellationToken.None)
        {
        }

        /// <summary>
        /// Creates a server that receives MSBuild events over a specified pipe.
        /// </summary>
        /// <param name="pipeStream">The pipe to receive events from.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that will cancel read operations if triggered.</param>
        public PipeLoggerServer(PipeStream pipeStream, CancellationToken cancellationToken)
        {
            PipeStream = pipeStream;
            _binaryReader = new BinaryReader(Buffer);
            _buildEventArgsReader = new BuildEventArgsReaderProxy(_binaryReader);
            CancellationToken = cancellationToken;

            // Dispose the pipe if cancelled, which will trigger cleanup and the final BinaryLogRecordKind.EndOfFile
            cancellationToken.Register(() => pipeStream.Dispose());

            new Thread(() =>
            {
                try
                {
                    Connect();
                    while(Buffer.Write(PipeStream))
                    {
                    }
                }
                catch (EndOfStreamException)
                {
                    // The client broke the stream so we're done
                }
                catch (ObjectDisposedException)
                {
                    // The pipe was disposed
                }
                catch (OperationCanceledException)
                {
                    // A cancellation bubbled up (probably unhandled from Connect() implementation)
                }

                Disconnect();

                // Add a final 0 (BinaryLogRecordKind.EndOfFile) into the stream in case the BuildEventArgsReader is waiting for a read
                Buffer.Write(new byte[1] { 0 }, 0, 1);

                Buffer.CompleteAdding();
            }).Start();
        }

        protected virtual void Connect()
        {
        }

        protected virtual void Disconnect()
        {
        }

        /// <summary>
        /// Reads a single event from the pipe. This method blocks until an event is received,
        /// there are no more events, or the pipe is closed.
        /// </summary>
        /// <returns><c>true</c> if an event was read, <c>false</c> otherwise.</returns>
        public bool Read()
        {
            if(Buffer.IsCompleted)
            {
                return false;
            }

            BuildEventArgs args = _buildEventArgsReader.Read();
            if (args != null)
            {
                Dispatch(args);
                return true;
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
