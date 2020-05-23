
namespace Swish.Sftp.Packets
{
    // See https://tools.ietf.org/html/rfc4254#section-5.3
    public class ChannelClose : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_CHANNEL_CLOSE; } }

        public uint RecipientChannel { get; set; }


        public override void Load(ByteReader reader)
        {
            RecipientChannel = reader.GetUInt32();
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(RecipientChannel);
        }
    }
}
