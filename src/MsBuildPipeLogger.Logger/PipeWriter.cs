using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger.Logger
{
    internal abstract class PipeWriter : IDisposable
    {
        private readonly PipeStream _pipe;
        private readonly BinaryWriter _binaryWriter;
        private readonly BuildEventArgsWriter _argsWriter;

        protected PipeWriter(PipeStream pipe)
        {
            _pipe = pipe;
            _binaryWriter = new BinaryWriter(_pipe);
            _argsWriter = new BuildEventArgsWriter(_binaryWriter);
        }

        public void Dispose()
        {
            try
            {
                _pipe.WaitForPipeDrain();
                _binaryWriter.Dispose();
                _pipe.Dispose();
            }
            catch { }
        }

        public void Write(BuildEventArgs e) => _argsWriter.Write(e);
    }
}
