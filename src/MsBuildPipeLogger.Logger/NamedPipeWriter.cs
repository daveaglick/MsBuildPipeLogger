using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    internal class NamedPipeWriter : PipeWriter
    {
        public string ServerName { get; }

        public string PipeName { get; }

        public NamedPipeWriter(string pipeName)
            : this(".", pipeName)
        {
        }

        public NamedPipeWriter(string serverName, string pipeName)
        {
            ServerName = serverName;
            PipeName = pipeName;
        }

        protected override PipeStream InitializePipe() =>
            new NamedPipeClientStream(ServerName, PipeName, PipeDirection.Out);
    }
}
