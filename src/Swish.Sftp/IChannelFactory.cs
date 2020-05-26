
using Swish.Sftp.Packets;

namespace Swish.Sftp
{
    public interface IChannelFactory
    {
        Channel Create(IPacketSender packetSender, ChannelOpen openPacket, uint channelId);
    }
}
