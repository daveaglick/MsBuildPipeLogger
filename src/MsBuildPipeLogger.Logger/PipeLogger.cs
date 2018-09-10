using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// Logger to send messages from the MSBuild logging system over an anonymous pipe.
    /// </summary>
    /// <remarks>
    /// Heavily based on the work of Kirill Osenkov and the MSBuildStructuredLog project.
    /// </remarks>
    public class PipeLogger : Microsoft.Build.Utilities.Logger
    {
        private PipeWriter _pipe;

        public override void Initialize(IEventSource eventSource)
        {
            Environment.SetEnvironmentVariable("MSBUILDTARGETOUTPUTLOGGING", "true");
            Environment.SetEnvironmentVariable("MSBUILDLOGIMPORTS", "1");

            _pipe = ParameterParser.GetPipeFromParameters(Parameters);
            eventSource.AnyEventRaised += (_, e) => _pipe.Write(e);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _pipe.Dispose();
        }
    }
}
