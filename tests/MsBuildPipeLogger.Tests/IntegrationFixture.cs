using Microsoft.Build.Framework;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MsBuildPipeLogger.Tests
{
    [TestFixture]
    public class IntegrationFixture
    {
        [Test]
        public void SendsDataOverAnonymousPipe()
        {
            // Given
            BuildEventArgs eventArgs = null;
            using (AnonymousPipeLoggerServer server = new AnonymousPipeLoggerServer())
            {
                server.AnyEventRaised += (s, e) => eventArgs = e;

                // When
                RunClientProcess(server, server.GetClientHandle());
            }

            // Then
            eventArgs.ShouldNotBeNull();
            eventArgs.ShouldBeOfType<BuildStartedEventArgs>();
            eventArgs.Message.ShouldBe("Testing 123");
        }

        [Test]
        public void SendsDataOverNamedPipe()
        {
            // Given
            BuildEventArgs eventArgs = null;
            using (NamedPipeLoggerServer server = new NamedPipeLoggerServer("foo"))
            {
                server.AnyEventRaised += (s, e) => eventArgs = e;

                // When
                RunClientProcess(server, "name=foo");
            }

            // Then
            eventArgs.ShouldNotBeNull();
            eventArgs.ShouldBeOfType<BuildStartedEventArgs>();
            eventArgs.Message.ShouldBe("Testing 123");
        }

        private void RunClientProcess(PipeLoggerServer server, string arguments)
        {
            Process process = new Process();
            try
            {
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $"MsBuildPipeLogger.Tests.Client.dll {arguments}";
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(typeof(IntegrationFixture).Assembly.Location).Replace("MsBuildPipeLogger.Tests", "MsBuildPipeLogger.Tests.Client");
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += (s, e) => TestContext.WriteLine(e.Data);
                process.ErrorDataReceived += (s, e) => TestContext.WriteLine(e.Data);

                process.Start();
                TestContext.WriteLine($"Started process {process.Id}");
                process.BeginOutputReadLine();

                while (!process.HasExited)
                {
                    server.Read();
                }
            }
            finally
            {
                process.Close();
            }
        }
    }
}
