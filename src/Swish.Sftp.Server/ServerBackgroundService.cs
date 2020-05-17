
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Swish.Sftp.Server
{
    public class ServerBackgroundService : IHostedService
    {
        private readonly CancellationTokenSource stoppingContext = new CancellationTokenSource();
        private readonly ILogger logger;
        private Task executingTask;
        private ISftpServer server;


        public ServerBackgroundService(ISftpServer server,
                                       ILogger<ServerBackgroundService> logger)
        {
            this.server = server;
            this.logger = logger;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("StartAsync");

            // Store the task we'll execute
            executingTask = RunServer(stoppingContext.Token);

            // If the task has already completed, just return it so that cancellations and failure
            // bubble back up to the caller.
            if (executingTask.IsCompleted)
            {
                return executingTask;
            }

            // Task is running.
            return Task.CompletedTask;
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("StopAsync");

            // If stop has been called without calling start first, we're done.
            if (executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                stoppingContext.Cancel();
            }
            finally
            {
                // Wait until the executing task completes, or the cancellation token trips.
                await Task.WhenAny(executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }


        private async Task RunServer(CancellationToken cancellationToken)
        {
            await server.Run(cancellationToken);
        }
    }
}
