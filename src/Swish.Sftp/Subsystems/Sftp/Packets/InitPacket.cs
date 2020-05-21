
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class InitPacket : SftpPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_INIT; } }

        public uint Version { get; private set; }

        public override void Load(ByteReader reader)
        {
            Version = reader.GetUInt32();
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Server should never send a SSH_FXP_INIT message");
        }
    }
}
