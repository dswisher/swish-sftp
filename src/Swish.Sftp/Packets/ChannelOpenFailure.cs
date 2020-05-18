
using System.Text;


namespace Swish.Sftp.Packets
{
    // See https://tools.ietf.org/html/rfc4254#section-5.1
    public class ChannelOpenFailure : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE; } }

        public uint RecipientChannel { get; set; }
        public uint ReasonCode { get; set; }        // TODO - make an enum?
        public string Description { get; set; }
        public string Language { get; set; } = "en";


        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_MSG_CHANNEL_OPEN_FAILURE message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(RecipientChannel);
            writer.WriteUInt32(ReasonCode);
            writer.WriteString(Description, Encoding.UTF8);
            writer.WriteString(Language);
        }
    }
}
