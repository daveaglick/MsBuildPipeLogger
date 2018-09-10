using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    public class NamedPipeLoggerServer : PipeLoggerServer
    {
        private bool _connected;

        public NamedPipeLoggerServer(string pipeName)
            : base(new NamedPipeServerStream(pipeName, PipeDirection.In))
        {
        }

        public override bool Read()
        {
            if (!_connected)
            {
                ((NamedPipeServerStream)PipeStream).WaitForConnection();
                _connected = true;
            }

            return base.Read();
        }
    }
}
