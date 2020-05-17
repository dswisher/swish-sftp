
using System.Net.Sockets;

using Microsoft.Extensions.Logging;


namespace Swish.Sftp
{
    public class ClientFactory
    {
        private readonly ILogger<Client> clientLogger;

        public ClientFactory(ILogger<Client> clientLogger)
        {
            this.clientLogger = clientLogger;
        }


        public Client CreateClient(Socket socket)
        {
            return new Client(socket, clientLogger);
        }
    }
}
