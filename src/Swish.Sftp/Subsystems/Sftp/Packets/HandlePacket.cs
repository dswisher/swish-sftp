
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class HandlePacket : ServerToClientPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_HANDLE; } }

        public uint Id { get; set; }
        public string Handle { get; set; }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(Id);
            writer.WriteString(Handle);
        }
    }
}
