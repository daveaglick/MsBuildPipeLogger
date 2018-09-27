using System;
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
            : base(new NamedPipeServerStream(pipeName, PipeDirection.In, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous), cancellationToken)
        {
        }

        protected override void Connect()
        {
            try
            {
                ((NamedPipeServerStream)PipeStream).WaitForConnectionAsync(CancellationToken).Wait();
            }
            catch(Exception)
            {
                // If something went wrong here, just close it up
                // This will always throw when the CancellationToken triggers pipe disposal on cancel handler
                Disconnect();
            }
        }

        protected override void Disconnect()
        {
            if (PipeStream.IsConnected)
            {
                ((NamedPipeServerStream)PipeStream).Disconnect();
            }
        }
    }
}
