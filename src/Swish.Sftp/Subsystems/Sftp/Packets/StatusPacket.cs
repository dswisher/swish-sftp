
using System.Collections.Generic;


namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class StatusPacket : ServerToClientPacket
    {
        public const uint Ok = 0;
        public const uint Eof = 1;
        public const uint NoSuchFile = 2;
        public const uint PermissionDenied = 3;
        public const uint Failure = 4;
        public const uint BadMessage = 5;
        public const uint Unsupported = 8;

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


        protected override IEnumerable<string> InternalGetDetails()
        {
            yield return $"StatusCode={StatusCode}";
            yield return $"ErrorMessage='{ErrorMessage}'";
        }
    }
}
