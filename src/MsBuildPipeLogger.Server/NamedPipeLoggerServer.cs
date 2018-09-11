using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// A server for receiving MSBuild logging events over a named pipe.
    /// </summary>
    public class NamedPipeLoggerServer : PipeLoggerServer
    {
        private bool _connected;

        /// <summary>
        /// Creates a named pipe server for receiving MSBuild logging events.
        /// </summary>
        /// <param name="pipeName">The name of the pipe to create.</param>
        public NamedPipeLoggerServer(string pipeName)
            : base(new NamedPipeServerStream(pipeName, PipeDirection.In))
        {
        }

        /// <inheritdoc />
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
