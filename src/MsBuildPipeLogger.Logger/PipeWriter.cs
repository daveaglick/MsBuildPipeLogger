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
        private readonly BlockingCollection<BuildEventArgs> _queue =
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
                BuildEventArgs eventArgs;
                while((eventArgs = TakeEventArgs()) != null)
                {
                    _argsWriter.Write(eventArgs);
                    _binaryWriter.Flush();
                    _pipeStream.Flush();
                }
                _doneProcessing.Set();
            }).Start();
        }

        private BuildEventArgs TakeEventArgs()
        {
            if (!_queue.IsCompleted)
            {
                try
                {
                    return _queue.Take();
                }
                catch (InvalidOperationException)
                {
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (!_queue.IsAddingCompleted)
            {
                try
                {
                    _queue.CompleteAdding();
                    _doneProcessing.WaitOne();
                    _pipeStream.WaitForPipeDrain();
                    _pipeStream.Dispose();
                }
                catch { }
            }
        }

        public void Write(BuildEventArgs e) => _queue.Add(e);
    }
}
