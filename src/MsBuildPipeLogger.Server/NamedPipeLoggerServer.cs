using System.IO.Pipes;
using System.Threading;

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
            : this(pipeName, CancellationToken.None)
        {
        }

        /// <summary>
        /// Creates a named pipe server for receiving MSBuild logging events.
        /// </summary>
        /// <param name="pipeName">The name of the pipe to create.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that will cancel read operations if triggered.</param>
        public NamedPipeLoggerServer(string pipeName, CancellationToken cancellationToken)
            : base(new NamedPipeServerStream(pipeName, PipeDirection.In), cancellationToken)
        {
        }

        protected override void Connect() =>
            ((NamedPipeServerStream)PipeStream).WaitForConnectionAsync(CancellationToken).Wait(CancellationToken);
    }
}
