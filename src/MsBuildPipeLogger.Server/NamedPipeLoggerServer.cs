using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// A server for receiving MSBuild logging events over a named pipe.
    /// </summary>
    public class NamedPipeLoggerServer : PipeLoggerServer
    {
        /// <summary>
        /// Creates a named pipe server for receiving MSBuild logging events.
        /// </summary>
        /// <param name="pipeName">The name of the pipe to create.</param>
        public NamedPipeLoggerServer(string pipeName)
            : base(new NamedPipeServerStream(pipeName, PipeDirection.In))
        {
        }

        protected override void Connect() =>
            ((NamedPipeServerStream)PipeStream).WaitForConnection();
    }
}
