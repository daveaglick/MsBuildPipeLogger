using Microsoft.Build.Framework;
using System;
using System.Diagnostics;

namespace MsBuildPipeLogger.Tests.Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(string.Join("; ", args));
            try
            {
                using (PipeWriter writer = ParameterParser.GetPipeFromParameters(args[0]))
                {
                    writer.Write(new BuildStartedEventArgs("Testing 123", "help"));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }
    }
}
