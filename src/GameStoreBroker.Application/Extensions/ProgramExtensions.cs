// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Config;
using GameStoreBroker.Application.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Extensions
{
    internal static class ProgramExtensions
    {
        public static Command AddOperationHandler<T>(this Command command) where T : Operation
        {
            command.Handler = CommandHandler.Create<IHost, CancellationToken>(RunOperationAsync<T>);
            return command;
        }

        private static async Task<int> RunOperationAsync<T>(IHost host, CancellationToken ct) where T : Operation
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            try
            {
                return await host.Services.GetRequiredService<T>().RunAsync(ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                logger.LogTrace(e, "Exception thrown.");
                return 2;
            }
        }

        public static Command AddGenerateConfigTemplateOperationHandler(this Command command)
        {
            command.Handler = CommandHandler.Create<IHost, Operations.Operations, CancellationToken>(RunGenerateConfigTemplateOperationAsync);
            return command;
        }

        private static async Task<int> RunGenerateConfigTemplateOperationAsync(IHost host, Operations.Operations operation, CancellationToken ct)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            try
            {
                var service = host.Services.GetRequiredService<GenerateConfigTemplateOperation>();
                return await service.RunAsync(operation, ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
                logger.LogTrace(e, "Exception thrown.");
                return 2;
            }
        }

        public static T GetOptionValue<T>(this InvocationContext invocationContext, Option<T> option)
        {
            return invocationContext.ParseResult.ValueForOption(option);
        }

        public static Option<T> Required<T>(this Option<T> option, bool required = true)
        {
            option.IsRequired = required;
            return option;
        }

        public static void AddOperation<T1, T2>(this IServiceCollection services, HostBuilderContext context) where T1 : Operation where T2 : BaseOperationConfig
        {
            services.AddScoped<T1>();
            services.AddOptions<T2>().Bind(context.Configuration).ValidateDataAnnotations();
        }

        public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, FileInfo configFile, Program.ConfigFileFormat configFileFormat) =>
            configFileFormat switch
            {
                Program.ConfigFileFormat.Json => builder.AddJsonFile(configFile.FullName, false, false),
                Program.ConfigFileFormat.Xml => builder.AddXmlFile(configFile.FullName, false, false),
                Program.ConfigFileFormat.Ini => builder.AddIniFile(configFile.FullName, false, false),
                _ => throw new ArgumentOutOfRangeException(nameof(configFileFormat), "Invalid config file format."),
            };
    }
}
