
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class ClosePacket : ClientToServerPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_CLOSE; } }

        public uint Id { get; set; }
        public string Handle { get; set; }

        public override void Load(ByteReader reader)
        {
            Id = reader.GetUInt32();
            Handle = reader.GetString();
        }
    }
}
