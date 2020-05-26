
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Swish.Sftp.Packets;
using Swish.Sftp.Subsystems.Sftp;

namespace Swish.Sftp
{
    public class ChannelFactory : IChannelFactory
    {
        private readonly IConfiguration config;
        private readonly IVirtualFileSystemFactory fileSystemFactory;
        private readonly ILoggerFactory loggerFactory;


        public ChannelFactory(IConfiguration config,
                              IVirtualFileSystemFactory fileSystemFactory,
                              ILoggerFactory loggerFactory)
        {
            this.config = config;
            this.fileSystemFactory = fileSystemFactory;
            this.loggerFactory = loggerFactory;
        }


        public Channel Create(IPacketSender packetSender, ChannelOpen openPacket, uint channelId)
        {
            var logger = loggerFactory.CreateLogger<Channel>();

            return new Channel(packetSender, config, fileSystemFactory, logger, channelId, openPacket);
        }
    }
}
