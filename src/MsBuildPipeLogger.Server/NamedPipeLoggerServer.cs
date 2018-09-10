using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    public class NamedPipeLoggerServer : PipeLoggerServer
    {
        public NamedPipeLoggerServer(string pipeName)
            : base(new NamedPipeServerStream(pipeName, PipeDirection.In))
        {
        }
    }
}
