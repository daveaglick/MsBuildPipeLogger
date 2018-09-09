using System.IO.Pipes;

namespace MsBuildPipeLogger.Logger
{
    internal class NamedPipeWriter : PipeWriter
    {
        public NamedPipeWriter(string pipeName)
            : this(".", pipeName)
        {
        }

        public NamedPipeWriter(string serverName, string pipeName)
            : base(new NamedPipeClientStream(serverName, pipeName, PipeDirection.Out))
        {
        }
    }
}
