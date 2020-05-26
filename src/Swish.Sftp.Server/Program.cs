
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Swish.Sftp.Subsystems.Sftp;

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
            var builder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<ISftpServer, SftpServer>();

                    // TODO - add a .Using method to set up all the dependencies?

                    services.AddSingleton<ClientFactory>();
                    services.AddSingleton<IChannelFactory, ChannelFactory>();
                    services.AddSingleton<IVirtualFileSystemFactory, VirtualFileSystemFactory>();

                    services.AddHostedService<ServerBackgroundService>();
                });

            return builder;
        }
    }
}
