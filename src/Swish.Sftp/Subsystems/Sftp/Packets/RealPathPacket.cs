
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class RealPathPacket : SftpPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_REALPATH; } }

        public uint Id { get; set; }
        public string Path { get; set; }

        public override void Load(ByteReader reader)
        {
            Id = reader.GetUInt32();
            Path = reader.GetString();
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Server should never send a SSH_FXP_INIT message");
        }
    }
}
