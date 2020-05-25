
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public abstract class ServerToClientPacket : SftpPacket
    {
        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, $"Client should never send a {PacketType} message");
        }
    }
}
