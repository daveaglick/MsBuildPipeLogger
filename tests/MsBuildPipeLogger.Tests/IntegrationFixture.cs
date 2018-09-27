using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MsBuildPipeLogger.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class IntegrationFixture
    {
        public static int[] MessageCounts = { 0, 1, 100000 };

        [Test]
        public void SerializesData([ValueSource(nameof(MessageCounts))] int messageCount)
        {
            // Given
            Stopwatch sw = new Stopwatch();
            MemoryStream memory = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memory);
            BuildEventArgsWriter writer = new BuildEventArgsWriter(binaryWriter);
            BinaryReader binaryReader = new BinaryReader(memory);
            BuildEventArgsReaderProxy reader = new BuildEventArgsReaderProxy(binaryReader);
            List<BuildEventArgs> eventArgs = new List<BuildEventArgs>();

            // When
            sw.Start();
            writer.Write(new BuildStartedEventArgs($"Testing", "help"));
            for (int m = 0; m < messageCount; m++)
            {
                writer.Write(new BuildMessageEventArgs($"Testing {m}", "help", "sender", MessageImportance.Normal));
            }
            sw.Stop();
            TestContext.Out.WriteLine($"Serialization completed in {sw.ElapsedMilliseconds} ms");

            memory.Position = 0;
            sw.Restart();
            BuildEventArgs e;
            while ((e = reader.Read()) != null)
            {
                eventArgs.Add(e);
                if (memory.Position >= memory.Length)
                {
                    break;
                }
            }
            sw.Stop();
            TestContext.Out.WriteLine($"Deserialization completed in {sw.ElapsedMilliseconds} ms");

            // Then
            eventArgs.Count.ShouldBe(messageCount + 1);
            eventArgs.First().ShouldBeOfType<BuildStartedEventArgs>();
            eventArgs.First().Message.ShouldBe("Testing");
            int c = 0;
            foreach (BuildEventArgs eventArg in eventArgs.Skip(1))
            {
                eventArg.ShouldBeOfType<BuildMessageEventArgs>();
                eventArg.Message.ShouldBe($"Testing {c++}");
            }
        }

        [Test]
        public void AnonymousPipeSupportsCancellation()
        {
            // Given
            bool read = false;
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                using (AnonymousPipeLoggerServer server = new AnonymousPipeLoggerServer(tokenSource.Token))
                {
                    // When
                    tokenSource.CancelAfter(1000);  // The call to .Read() below will block so need to set a timeout for cancellation
                    read = server.Read();
                }
            }

            // Then
            read.ShouldBe(false);
        }

        [Test]
        public void NamedPipeSupportsCancellation()
        {
            // Given
            bool read = false;
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                using (NamedPipeLoggerServer server = new NamedPipeLoggerServer("Foo", tokenSource.Token))
                {
                    // When
                    tokenSource.CancelAfter(1000);  // The call to .Read() below will block so need to set a timeout for cancellation
                    read = server.Read();
                }
            }

            // Then
            read.ShouldBe(false);
        }

        [Test]
        public void SendsDataOverAnonymousPipe([ValueSource(nameof(MessageCounts))] int messageCount)
        {
            // Given
            List<BuildEventArgs> eventArgs = new List<BuildEventArgs>();
            int exitCode;
            using (AnonymousPipeLoggerServer server = new AnonymousPipeLoggerServer())
            {
                server.AnyEventRaised += (s, e) => eventArgs.Add(e);

                // When
                exitCode = RunClientProcess(server, server.GetClientHandle(), messageCount);
            }

            // Then
            exitCode.ShouldBe(0);
            eventArgs.Count.ShouldBe(messageCount + 1);
            eventArgs.First().ShouldBeOfType<BuildStartedEventArgs>();
            eventArgs.First().Message.ShouldBe("Testing");
            int c = 0;
            foreach (BuildEventArgs eventArg in eventArgs.Skip(1))
            {
                eventArg.ShouldBeOfType<BuildMessageEventArgs>();
                eventArg.Message.ShouldBe($"Testing {c++}");
            }
        }

        [Test]
        public void SendsDataOverNamedPipe([ValueSource(nameof(MessageCounts))] int messageCount)
        {
            // Given
            List<BuildEventArgs> eventArgs = new List<BuildEventArgs>();
            int exitCode;
            using (NamedPipeLoggerServer server = new NamedPipeLoggerServer("foo"))
            {
                server.AnyEventRaised += (s, e) => eventArgs.Add(e);

                // When
                exitCode = RunClientProcess(server, "name=foo", messageCount);
            }

            // Then
            exitCode.ShouldBe(0);
            eventArgs.Count.ShouldBe(messageCount + 1);
            eventArgs.First().ShouldBeOfType<BuildStartedEventArgs>();
            eventArgs.First().Message.ShouldBe("Testing");
            int c = 0;
            foreach (BuildEventArgs eventArg in eventArgs.Skip(1))
            {
                eventArg.ShouldBeOfType<BuildMessageEventArgs>();
                eventArg.Message.ShouldBe($"Testing {c++}");
            }
        }

        private int RunClientProcess(IPipeLoggerServer server, string arguments, int messages)
        {
            Process process = new Process();
            int exitCode;
            try
            {
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = $"MsBuildPipeLogger.Tests.Client.dll {arguments} {messages}";
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

                while (server.Read())
                {
                }
            }
            catch(Exception ex)
            {
                TestContext.WriteLine($"Process error: {ex.ToString()}");
            }
            finally
            {
                process.WaitForExit();
                exitCode = process.ExitCode;
                TestContext.WriteLine($"Exited process {process.Id} with code {exitCode}");
                process.Close();
            }
            return exitCode;
        }
    }
}
