
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;


namespace Swish.Sftp.Server
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Setting up and running host...");

                CreateHostBuilder(args).Build().Run();

                Log.Information("Host shut down cleanly.");

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }


        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var settings = new ServerSettings
            {
                Port = 22
            };

            var builder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<ISftpServer, SftpServer>();

                    // TODO - add a .Using method to set up all the dependencies?

                    services.AddSingleton<ServerSettings>(settings);
                    services.AddSingleton<ClientFactory>();

                    services.AddHostedService<ServerBackgroundService>();
                });

            return builder;
        }
    }
}
