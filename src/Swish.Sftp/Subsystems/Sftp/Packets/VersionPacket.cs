
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class VersionPacket : SftpPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_VERSION; } }

        public uint Version { get; set; }

        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_FXP_VERSION message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(Version);
        }
    }
}
