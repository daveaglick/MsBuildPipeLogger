using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    internal class AnonymousPipeWriter : PipeWriter
    {
        public string Handle { get; }

        public AnonymousPipeWriter(string pipeHandleAsString)
        {
            Handle = pipeHandleAsString;
        }

        protected override PipeStream InitializePipe() =>
            new AnonymousPipeClientStream(PipeDirection.Out, Handle);
    }
}
