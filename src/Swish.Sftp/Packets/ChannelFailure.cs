
namespace Swish.Sftp.Packets
{
    public class ChannelFailure : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_CHANNEL_FAILURE; } }

        public uint RecipientChannel { get; set; }


        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_MSG_CHANNEL_FAILURE message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(RecipientChannel);
        }
    }
}
