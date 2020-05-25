
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class OpenDirPacket : ClientToServerPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_OPENDIR; } }

        public uint Id { get; set; }
        public string Path { get; set; }

        public override void Load(ByteReader reader)
        {
            Id = reader.GetUInt32();
            Path = reader.GetString();
        }
    }
}
