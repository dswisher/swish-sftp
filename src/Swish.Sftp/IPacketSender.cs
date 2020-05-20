
using Swish.Sftp.Packets;

namespace Swish.Sftp
{
    public interface IPacketSender
    {
        void Send(Packet packet);
    }
}
