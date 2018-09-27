using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// A server for receiving MSBuild logging events over an anonymous pipe.
    /// </summary>
    public class AnonymousPipeLoggerServer : PipeLoggerServer
    {
        private string _clientHandle;

        /// <summary>
        /// Creates an anonymous pipe server for receiving MSBuild logging events.
        /// </summary>
        public AnonymousPipeLoggerServer()
            : this(CancellationToken.None)
        {
        }

        /// <summary>
        /// Creates an anonymous pipe server for receiving MSBuild logging events.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that will cancel read operations if triggered.</param>
        public AnonymousPipeLoggerServer(CancellationToken cancellationToken)
            : base(new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable), cancellationToken)
        {
        }

        /// <summary>
        /// Gets the client handle as a string. The local copy of the handle will be automatically disposed
        /// on the first call to <see cref="Read"/>.
        /// </summary>
        /// <returns>The client handle as a string.</returns>
        public string GetClientHandle() =>
            _clientHandle ?? (_clientHandle = ((AnonymousPipeServerStream)PipeStream).GetClientHandleAsString());

        protected override void Connect()
        {
            // Wait for the pipe to send something so we know we're connected
            Buffer.Write(PipeStream);

            // This doesn't actually disconnect, it just disposes the client handle
            Disconnect();
        }

        protected override void Disconnect()
        {
            // Dispose the client handle if we asked for one
            // If we don't do this we won't get notified when the stream closes, see https://stackoverflow.com/q/39682602/807064
            if (_clientHandle != null)
            {
                ((AnonymousPipeServerStream)PipeStream).DisposeLocalCopyOfClientHandle();
                _clientHandle = null;
            }
        }
    }
}
