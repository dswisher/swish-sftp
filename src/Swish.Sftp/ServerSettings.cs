
namespace Swish.Sftp
{
    public class ServerSettings
    {
        public ServerSettings()
        {
            Port = 22;
            MaxPendingConnections = 64;
        }


        public int Port { get; set; }
        public int MaxPendingConnections { get; set; }
    }
}
