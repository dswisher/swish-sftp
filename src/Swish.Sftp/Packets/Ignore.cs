
namespace Swish.Sftp.Packets
{
    public class Ignore : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_IGNORE; } }


        public override void Load(ByteReader reader)
        {
            // No data
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // No data
        }
    }
}
