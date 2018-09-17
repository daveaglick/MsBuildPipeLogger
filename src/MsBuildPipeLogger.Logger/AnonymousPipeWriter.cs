using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    internal class AnonymousPipeWriter : PipeWriter
    {
        public string Handle { get; }

        public AnonymousPipeWriter(string pipeHandleAsString)
            : base(new AnonymousPipeClientStream(PipeDirection.Out, pipeHandleAsString))
        {
            Handle = pipeHandleAsString;
        }
    }
}
