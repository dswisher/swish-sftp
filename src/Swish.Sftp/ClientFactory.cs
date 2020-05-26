
using System.Net.Sockets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Swish.Sftp
{
    public class ClientFactory
    {
        private readonly IConfiguration config;
        private readonly IChannelFactory channelFactory;
        private readonly ILogger<Client> clientLogger;


        public ClientFactory(IChannelFactory channelFactory,
                             IConfiguration config,
                             ILogger<Client> clientLogger)
        {
            this.config = config;
            this.channelFactory = channelFactory;
            this.clientLogger = clientLogger;
        }


        public Client CreateClient(Socket socket)
        {
            return new Client(config, socket, channelFactory, clientLogger);
        }
    }
}
