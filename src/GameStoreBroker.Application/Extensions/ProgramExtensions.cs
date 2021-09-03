// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GameStoreBroker.Application.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application.Extensions
{
    internal static class ProgramExtensions
    {
        public static Command AddOperationHandler<T>(this Command command) where T : Operation
        {
            command.Handler = CommandHandler.Create<IHost, CancellationToken>(RunAsyncOperation<T>);
            return command;
        }

        private static async Task<int> RunAsyncOperation<T>(IHost host, CancellationToken ct) where T : Operation
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

        public static T GetOptionValue<T>(this InvocationContext invocationContext, Option<T> option)
        {
            return invocationContext.ParseResult.ValueForOption(option);
        }

        public static void AddOperation<T1, T2>(this IServiceCollection services, HostBuilderContext context) where T1 : Operation where T2 : class
        {
            services.AddScoped<T1>();
            services.AddOptions<T2>().Bind(context.Configuration.GetSection("GameStoreBroker")).ValidateDataAnnotations();
        }
    }
}