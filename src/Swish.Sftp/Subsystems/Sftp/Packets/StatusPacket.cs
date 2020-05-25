

namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class StatusPacket : ServerToClientPacket
    {
        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_STATUS; } }

        public uint Id { get; set; }
        public uint StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public string Language { get; set; } = "en";


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(Id);
            writer.WriteUInt32(StatusCode);
            writer.WriteString(ErrorMessage);
            writer.WriteString(Language);
        }
    }
}
