
namespace Swish.Sftp.Packets
{
    public class ChannelOpenConfirmation : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION; } }

        public uint RecipientChannel { get; set; }
        public uint SenderChannel { get; set; }
        public uint InitialWindowSize { get; set; }
        public uint MaximumPacketSize { get; set; }


        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_MSG_CHANNEL_OPEN_CONFIRMATION message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(RecipientChannel);
            writer.WriteUInt32(SenderChannel);
            writer.WriteUInt32(InitialWindowSize);
            writer.WriteUInt32(MaximumPacketSize);
        }
    }
}
