﻿using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// A server for receiving MSBuild logging events over a named pipe.
    /// </summary>
    public class NamedPipeLoggerServer : PipeLoggerServer<NamedPipeServerStream>
    {
        private readonly InterlockedBool _connected = new InterlockedBool(false);

        public string PipeName { get; }

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
            PipeName = pipeName;
            CancellationToken.Register(CancelConnectionWait);
        }

        protected override void Connect()
        {
            PipeStream.WaitForConnection();
            _connected.Set();
        }
        
        private void CancelConnectionWait()
        {
            if(!_connected.Set())
            {
                // This is a crazy hack that stops the WaitForConnection by connecting a dummy client
                // We have to do it this way instead of checking for .IsConnected because if we connect
                // and then disconnect very quickly, .IsConnected will never observe as true and we'll lock
                using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeStream.Connect();
                }
            }
        }
    }

    internal class InterlockedBool
    {
        private volatile int _set;

        public InterlockedBool()
        {
            _set = 0;
        }

        public InterlockedBool(bool initialState)
        {
            _set = initialState ? 1 : 0;
        }

        // Returns the previous switch state of the switch
        public bool Set()
        {
#pragma warning disable 420
            return Interlocked.Exchange(ref _set, 1) != 0;
#pragma warning restore 420
        }

        // Returns the previous switch state of the switch
        public bool Unset()
        {
#pragma warning disable 420
            return Interlocked.Exchange(ref _set, 0) != 0;
#pragma warning restore 420
        }

        // Returns the current state
        public static implicit operator bool(InterlockedBool interlockedBool)
        {
            return interlockedBool._set != 0;
        }
    }
}
