using GameStoreBroker.ClientApi;
using GameStoreBroker.FileLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameStoreBroker.Application
{
    internal static class Program
    {
        private const string LogTimestampFormat = "yyyy-MM-dd hh:mm:ss.fff ";

        private static async Task<int> Main(string[] args)
        {
            return await BuildCommandLine()
                .UseHost(hostBuilder => hostBuilder
                    .ConfigureLogging((_, logging) =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Warning);
                        logging.AddFilter("GameStoreBroker", args.Contains("-v") || args.Contains("--Verbose") ? LogLevel.Trace : LogLevel.Information);
                        logging.AddSimpleFile(options =>
                        {
                            options.IncludeScopes = true;
                            options.SingleLine = true;
                            options.TimestampFormat = LogTimestampFormat;
                        }, file =>
                        {
                            file.Path = Path.Combine(Path.GetTempPath(), $"GameStoreBroker_{DateTime.Now:yyyyMMddhhmmss}.log");
                        });
                        logging.AddSimpleConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.SingleLine = true;
                            options.TimestampFormat = LogTimestampFormat;
                        });
                    })
                    .ConfigureServices((_, services) =>
                    {
                        services.AddLogging();
                        services.AddGameStoreBrokerService();
                    }))
                .UseDefaults()
                .Build()
                .InvokeAsync(args)
                .ConfigureAwait(false);
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            // Options
            var configFile = new Option<string>(new[] {"-c", "--ConfigFile"}, "The location of json config file");
            var clientSecret = new Option<string>(new[] {"-s", "--ClientSecret"}, "The client secret of the AAD app.");
            var verbose = new Option<bool>(new[] {"-v", "--Verbose"}, "Log verbose messages such as http calls.");

            // Root Command
            var rootCommand = new RootCommand
            {
                verbose,
                new Command("GetProduct", "Gets metadata of the product.")
                {
                    configFile, clientSecret, verbose,
                }.AddHandler(CommandHandler.Create<IHost, Options, CancellationToken>(GetProduct)),
            };

            rootCommand.Description = "GameStoreBroker description.";
            return new CommandLineBuilder(rootCommand);
        }

        private static async Task<int> GetProduct(IHost host, Options options, CancellationToken ct) => 
            await new Commands.GetProduct(host, options).Run(ct).ConfigureAwait(false);
    }
}
