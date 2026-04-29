// Copyright(c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PackageUploader.Application.Extensions;
using PackageUploader.Application.Operations;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PackageUploader.Application
{
    internal class OperationInvoker<T> : AsynchronousCommandLineAction where T : Operation
    {
        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken ct)
        {
            var hostAppBuilder = parseResult.CreateHostApplicationBuilder();
            using var host = hostAppBuilder.Build();

            var logger = host.Services.GetRequiredService<ILogger<OperationInvoker<T>>>();
            var exitCode = 0;
            try
            {
                var version = GetVersion();
                logger.LogInformation("PackageUploader v.{version} is starting.", version);
                exitCode = await host.Services.GetRequiredService<T>().RunAsync(ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError("{errorMessage}", e.Message);
                logger.LogTrace(e, "Exception thrown.");
                exitCode = 2;
            }

            Environment.ExitCode = exitCode;
            return exitCode;
        }

        private static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (assemblyVersionAttribute is not null)
            {
                return assemblyVersionAttribute.InformationalVersion;
            }
            return assembly.GetName().Version?.ToString() ?? string.Empty;
        }
    }
}
