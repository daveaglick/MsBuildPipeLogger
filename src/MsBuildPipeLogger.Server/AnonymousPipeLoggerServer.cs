using System.IO;
using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    public class AnonymousPipeLoggerServer : PipeLoggerServer
    {
        private string _clientHandle;

        public AnonymousPipeLoggerServer()
            : base(new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
        {
        }

        public string GetClientHandle() =>
            _clientHandle ?? (_clientHandle = ((AnonymousPipeServerStream)PipeStream).GetClientHandleAsString());

        public override bool Read()
        {
            // First dispose the client handle if we asked for one
            // If we don't do this we won't get notified when the stream closes, see https://stackoverflow.com/q/39682602/807064
            if (_clientHandle != null)
            {
                ((AnonymousPipeServerStream)PipeStream).DisposeLocalCopyOfClientHandle();
                _clientHandle = null;
            }

            return base.Read();
        }
    }
}
