using System.IO.Pipes;

namespace MsBuildPipeLogger.Logger
{
    internal class AnonymousPipeWriter : PipeWriter
    {
        public AnonymousPipeWriter(string pipeHandleAsString)
            : base(new AnonymousPipeClientStream(PipeDirection.Out, pipeHandleAsString))
        {
        }
    }
}
