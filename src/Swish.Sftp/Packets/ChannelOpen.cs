
using System.Text;

namespace Swish.Sftp.Packets
{
    // See https://tools.ietf.org/html/rfc4254#section-5.1
    public class ChannelOpen : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_CHANNEL_OPEN; } }

        public string ChannelType { get; set; }
        public uint SenderChannel { get; set; }
        public uint InitialWindowSize { get; set; }
        public uint MaximumPacketSize { get; set; }


        public override void Load(ByteReader reader)
        {
            ChannelType = reader.GetString(Encoding.ASCII);
            SenderChannel = reader.GetUInt32();
            InitialWindowSize = reader.GetUInt32();
            MaximumPacketSize = reader.GetUInt32();
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Server should never send a SSH_MSG_CHANNEL_OPEN message");
        }
    }
}
