
using System.Text;


namespace Swish.Sftp.Packets
{
    public class ChannelRequest : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_CHANNEL_REQUEST; } }

        public uint RecipientChannel { get; set; }
        public string RequestType { get; set; }
        public bool WantReply { get; set; }

        // env
        public string VariableName { get; set; }
        public string VariableValue { get; set; }

        // subsystem
        public string SubsystemName { get; set; }


        public override void Load(ByteReader reader)
        {
            RecipientChannel = reader.GetUInt32();
            RequestType = reader.GetString(Encoding.ASCII);
            WantReply = reader.GetBoolean();

            if (RequestType == "env")
            {
                VariableName = reader.GetString(Encoding.UTF8);
                VariableValue = reader.GetString(Encoding.UTF8);
            }
            else if (RequestType == "subsystem")
            {
                SubsystemName = reader.GetString(Encoding.UTF8);
            }
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this - YET! But it will send variants at some point, maybe!
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Server should never send a SSH_MSG_CHANNEL_REQUEST message");
        }
    }
}
