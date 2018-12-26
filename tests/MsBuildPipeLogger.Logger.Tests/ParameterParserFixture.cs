using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using MsBuildPipeLogger;
using NUnit.Framework;
using Shouldly;

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
            KeyValuePair<ParameterParser.ParameterType, string>[] parts = ParameterParser.ParseParameters(parameters);

            // Then
            parts.ShouldBe(new KeyValuePair<ParameterParser.ParameterType, string>[]
            {
                new KeyValuePair<ParameterParser.ParameterType, string>(ParameterParser.ParameterType.Handle, "1234")
            });
        }

        [TestCase("name=Foo")]
        [TestCase("\"name=Foo\"")]
        [TestCase("NAME=Foo")]
        [TestCase(" name = Foo ")]
        public void GetsNamedPipe(string parameters)
        {
            // Given, When
            KeyValuePair<ParameterParser.ParameterType, string>[] parts = ParameterParser.ParseParameters(parameters);

            // Then
            parts.ShouldBe(new KeyValuePair<ParameterParser.ParameterType, string>[]
            {
                new KeyValuePair<ParameterParser.ParameterType, string>(ParameterParser.ParameterType.Name, "Foo")
            });
        }

        [TestCase("name=Foo;server=Bar")]
        [TestCase("\"name=Foo;server=Bar\"")]
        [TestCase("NAME=Foo;SERVER=Bar")]
        [TestCase(" name = Foo ; server = Bar")]
        public void GetsNamedPipeWithServer(string parameters)
        {
            // Given, When
            KeyValuePair<ParameterParser.ParameterType, string>[] parts = ParameterParser.ParseParameters(parameters);

            // Then
            parts.ShouldBe(
                new KeyValuePair<ParameterParser.ParameterType, string>[]
                {
                    new KeyValuePair<ParameterParser.ParameterType, string>(ParameterParser.ParameterType.Name, "Foo"),
                    new KeyValuePair<ParameterParser.ParameterType, string>(ParameterParser.ParameterType.Server, "Bar")
                },
                true);
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
