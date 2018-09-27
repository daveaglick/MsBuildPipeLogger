using System;

namespace MsBuildPipeLogger
{
    public interface IPipeLoggerServer : IDisposable
    {
        bool Read();
    }
}
