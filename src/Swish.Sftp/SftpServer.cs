
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Swish.Sftp
{
    public class SftpServer : ISftpServer
    {
        private readonly ClientFactory clientFactory;
        private readonly ServerSettings settings;
        private readonly ILogger logger;

        private readonly List<Client> clients = new List<Client>();

        private TcpListener listener;


        public SftpServer(ClientFactory clientFactory,
                          IConfiguration config,
                          ILogger<SftpServer> logger)
        {
            this.clientFactory = clientFactory;
            this.logger = logger;

            settings = config.GetSection("server").Get<ServerSettings>();
        }


        public async Task Run(CancellationToken cancellationToken)
        {
            // Start up
            logger.LogInformation("Creating listener on port {Port}.", settings.Port);

            listener = new TcpListener(IPAddress.Any, settings.Port);
            listener.Start(settings.MaxPendingConnections);

            // Run
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check for any new connections
                await AcceptNewConnectionsAsync();

                // Poll all the existing clients
                await PollClientsAsync(cancellationToken);

                // Clean up any disconnected clients
                clients.RemoveAll(c => !c.IsConnected);

                // Take a breath
                await Task.Delay(5, cancellationToken);
            }

            // Shut down
            logger.LogInformation("Stopping the listener.");

            listener.Stop();
            listener = null;

            foreach (var client in clients)
            {
                // TODO - async? cancel token?
                client.Disconnect(DisconnectReason.SSH_DISCONNECT_BY_APPLICATION, "The server is getting shutdown.");
            }
        }


        private async Task AcceptNewConnectionsAsync()
        {
            while (listener.Pending())
            {
                var socket = await listener.AcceptSocketAsync();
                var client = clientFactory.CreateClient(socket);

                clients.Add(client);

                logger.LogInformation("New client: {Id} from {Endpoint}", client.Id, socket.RemoteEndPoint);
            }
        }


        private async Task PollClientsAsync(CancellationToken cancellationToken)
        {
            foreach (var client in clients)
            {
                await client.PollAsync(cancellationToken);
            }
        }
    }
}
