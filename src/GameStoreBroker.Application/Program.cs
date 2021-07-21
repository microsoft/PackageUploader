using System;
using System.Threading.Tasks;
using GameStoreBroker.ClientApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameStoreBroker.Application
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .ConfigureAppConfiguration((_, builder) =>
                {
                    builder.SetBasePath(Environment.CurrentDirectory);
                    builder.AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddGameStoreBrokerService();
                }).Build();
            
            using var scope = host.Services.CreateScope();
            try
            {
                var storeBroker = scope.ServiceProvider.GetRequiredService<GameStoreBrokerService>();
                await storeBroker.UploadGame();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception found: ");
                Console.WriteLine(e);
                Console.WriteLine(string.Empty);
                Console.WriteLine(@"Press any Key to exit...");
                Console.ReadKey();
            }
        }
    }
}
