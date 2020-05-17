
namespace Swish.Sftp.Packets
{
    public class Unimplemented : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_UNIMPLEMENTED; } }
        public uint RejectedPacketNumber { get; set; }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(RejectedPacketNumber);
        }
    }
}
