
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class ReadDirPacket : ClientToServerPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_READDIR; } }

        public uint Id { get; set; }
        public string Handle { get; set; }


        public override void Load(ByteReader reader)
        {
            Id = reader.GetUInt32();
            Handle = reader.GetString();
        }
    }
}
