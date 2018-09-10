using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger
{
    internal abstract class PipeWriter : IDisposable
    {
        private PipeStream _pipeStream;
        private BinaryWriter _binaryWriter;
        private BuildEventArgsWriter _argsWriter;

        protected abstract PipeStream InitializePipe();

        public void Dispose()
        {
            if (_pipeStream != null)
            {
                try
                {
                    _pipeStream.WaitForPipeDrain();
                    _binaryWriter.Dispose();
                    _pipeStream.Dispose();
                }
                catch { }
                _pipeStream = null;
            }
        }

        public void Write(BuildEventArgs e)
        {
            if(_pipeStream == null)
            {
                _pipeStream = InitializePipe();
                if(_pipeStream == null)
                {
                    throw new LoggerException("Could not initialize pipe");
                }
                _binaryWriter = new BinaryWriter(_pipeStream);
                _argsWriter = new BuildEventArgsWriter(_binaryWriter);
            }
            _argsWriter.Write(e);
        }
    }
}
