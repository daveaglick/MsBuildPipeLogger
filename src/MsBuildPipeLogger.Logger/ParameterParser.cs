using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace MsBuildPipeLogger.Logger
{
    internal static class ParameterParser
    {
        public static PipeWriter GetPipeFromParameters(string rawParameters)
        {
            // Get the parameter components
            string[] parameters = rawParameters.Split(';');
            if(parameters.Length < 1 || parameters.Length > 2)
            {
                throw new LoggerException("Unexpected number of parameters");
            }

            // Process the parameters
            return ProcessParameters(parameters.Select(x => ParseParameter(x)).ToArray());
        }

        private static PipeWriter ProcessParameters(KeyValuePair<ParameterType, string>[] parameters)
        {
            if (parameters.Any(x => string.IsNullOrWhiteSpace(x.Value)))
            {
                throw new LoggerException($"Invalid or empty parameter value");
            }

            // Anonymous pipe
            if (parameters[0].Key == ParameterType.Handle)
            {
                if (parameters.Length > 1)
                {
                    throw new LoggerException("Handle can only be specified as a single parameter");
                }
                return new AnonymousPipeWriter(parameters[0].Value);
            }

            // Named pipe
            if(parameters[0].Key == ParameterType.Name)
            {
                if(parameters.Length == 1)
                {
                    return new NamedPipeWriter(parameters[0].Value);
                }
                if(parameters[1].Key != ParameterType.Server)
                {
                    throw new LoggerException("Only server and name can be specified for a named pipe");
                }
                return new NamedPipeWriter(parameters[1].Value, parameters[0].Value);
            }
            if(parameters.Length == 1 || parameters[1].Key != ParameterType.Name)
            {
                throw new LoggerException("Pipe name must be specified for a named pipe");
            }
            return new NamedPipeWriter(parameters[0].Value, parameters[1].Value);
        }

        private static KeyValuePair<ParameterType, string> ParseParameter(string parameter)
        {
            string[] parts = parameter.Trim().Trim('"').Split('=');

            // No parameter name specified
            if (parts.Length == 1)
            {
                return new KeyValuePair<ParameterType, string>(ParameterType.Handle, parts[0].Trim());
            }

            // Parse the parameter name
            ParameterType parameterType;
            if(!Enum.TryParse(parts[0].Trim(), true, out parameterType))
            {
                throw new LoggerException($"Invalid parameter name {parts[0]}");
            }
            return new KeyValuePair<ParameterType, string>(parameterType, string.Join("=", parts.Skip(1)).Trim());
        }

        private enum ParameterType
        {
            Handle,
            Name,
            Server
        }
    }
}
