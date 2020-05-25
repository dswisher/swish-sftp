
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public abstract class ClientToServerPacket : SftpPacket
    {
        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, $"Server should never send a {PacketType} message");
        }
    }
}
