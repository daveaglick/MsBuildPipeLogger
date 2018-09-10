using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger
{
    internal abstract class PipeWriter : IDisposable
    {
        private PipeStream _pipe;
        private BinaryWriter _binaryWriter;
        private BuildEventArgsWriter _argsWriter;

        protected abstract PipeStream InitializePipe();

        public void Dispose()
        {
            if (_pipe != null)
            {
                try
                {
                    _pipe.WaitForPipeDrain();
                    _binaryWriter.Dispose();
                    _pipe.Dispose();
                }
                catch { }
                _pipe = null;
            }
        }

        public void Write(BuildEventArgs e)
        {
            if(_pipe == null)
            {
                _pipe = InitializePipe();
                if(_pipe == null)
                {
                    throw new LoggerException("Could not initialize pipe");
                }
                _binaryWriter = new BinaryWriter(_pipe);
                _argsWriter = new BuildEventArgsWriter(_binaryWriter);
            }
            _argsWriter.Write(e);
        }
    }
}
