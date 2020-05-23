
using System.Net.Sockets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Swish.Sftp
{
    public class ClientFactory
    {
        private readonly IConfiguration config;
        private readonly ILogger<Client> clientLogger;


        public ClientFactory(IConfiguration config,
                             ILogger<Client> clientLogger)
        {
            this.config = config;
            this.clientLogger = clientLogger;
        }


        public Client CreateClient(Socket socket)
        {
            return new Client(config, socket, clientLogger);
        }
    }
}
