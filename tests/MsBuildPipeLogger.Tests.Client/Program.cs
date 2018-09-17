using Microsoft.Build.Framework;
using System;
using System.Diagnostics;

namespace MsBuildPipeLogger.Tests.Client
{
    public class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine(string.Join("; ", args));
            int messages = int.Parse(args[1]);
            try
            {
                using (PipeWriter writer = ParameterParser.GetPipeFromParameters(args[0]))
                {
                    writer.Write(new BuildStartedEventArgs($"Testing", "help"));
                    for (int c = 0; c < messages; c++)
                    {
                        writer.Write(new BuildMessageEventArgs($"Testing {c}", "help", "sender", MessageImportance.Normal));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
            return 0;
        }
    }
}
