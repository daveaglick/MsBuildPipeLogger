using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger
{
    internal abstract class PipeWriter : IDisposable
    {
        private readonly BlockingCollection<BuildEventArgs> _argsQueue =
            new BlockingCollection<BuildEventArgs>(new ConcurrentQueue<BuildEventArgs>());
        private readonly AutoResetEvent _doneProcessing = new AutoResetEvent(false);

        private readonly PipeStream _pipeStream;
        private readonly BinaryWriter _binaryWriter;
        private readonly BuildEventArgsWriter _argsWriter;
        
        protected PipeWriter(PipeStream pipeStream)
        {
            _pipeStream = pipeStream ?? throw new ArgumentNullException(nameof(pipeStream));
            _binaryWriter = new BinaryWriter(_pipeStream);
            _argsWriter = new BuildEventArgsWriter(_binaryWriter);

            new Thread(() =>
            {
                while (!_argsQueue.IsCompleted)
                {
                    BuildEventArgs eventArgs;
                    try
                    {
                        eventArgs = _argsQueue.Take();
                    }
                    catch(InvalidOperationException)
                    {
                        break;
                    }
                    _argsWriter.Write(eventArgs);
                    _pipeStream.Flush();
                }
                _doneProcessing.Set();
            }).Start();
        }

        public void Dispose()
        {
            if (!_argsQueue.IsAddingCompleted)
            {
                try
                {
                    _argsQueue.CompleteAdding();
                    _doneProcessing.WaitOne();
                    _pipeStream.WaitForPipeDrain();
                    _binaryWriter.Dispose();
                    _pipeStream.Dispose();
                }
                catch { }
            }
        }

        public void Write(BuildEventArgs e) => _argsWriter.Write(e);
    }
}
