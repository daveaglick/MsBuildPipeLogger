using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using MsBuildPipeLogger;
using Shouldly;
using Microsoft.Build.Framework;

namespace MsBuildPipeLogger.Logger.Tests
{
    [TestFixture]
    public class ParameterParserFixture
    {
        [TestCase("1234")]
        [TestCase(" 1234 ")]
        [TestCase("handle=1234")]
        [TestCase("HANDLE=1234")]
        [TestCase(" handle = 1234 ")]
        [TestCase("\"1234\"")]
        [TestCase("\"handle=1234\"")]
        [TestCase("\" handle = 1234 \"")]
        public void GetsAnonymousPipe(string parameters)
        {
            // Given, When
            AnonymousPipeWriter writer = ParameterParser.GetPipeFromParameters(parameters) as AnonymousPipeWriter;

            // Then
            writer.ShouldNotBeNull();
            writer.Handle.ShouldBe("1234");            
        }

        [TestCase("name=Foo")]
        [TestCase("\"name=Foo\"")]
        [TestCase("NAME=Foo")]
        [TestCase(" name = Foo ")]
        [TestCase("name=Foo;server=Bar")]
        [TestCase("\"name=Foo;server=Bar\"")]
        [TestCase("NAME=Foo;SERVER=Bar")]
        [TestCase(" name = Foo ; server = Bar")]
        public void GetsNamedPipe(string parameters)
        {
            // Given, When
            NamedPipeWriter writer = ParameterParser.GetPipeFromParameters(parameters) as NamedPipeWriter;

            // Then
            writer.ShouldNotBeNull();
            writer.PipeName.ShouldBe("Foo");
            writer.ServerName.ShouldBe(parameters.Contains("server", StringComparison.OrdinalIgnoreCase) ? "Bar" : ".");
        }

        [TestCase("")]
        [TestCase("  ")]
        [TestCase("123;foo;baz")]
        [TestCase("handle=1234;foo")]
        [TestCase("handle=1234;name=bar")]
        [TestCase("foo=bar")]
        [TestCase("123;name=bar")]
        [TestCase("server=foo")]
        public void ThrowsForInvalidParameters(string parameters)
        {
            // Given, When, Then
            Should.Throw<LoggerException>(() => ParameterParser.GetPipeFromParameters(parameters));            
        }
    }
}
