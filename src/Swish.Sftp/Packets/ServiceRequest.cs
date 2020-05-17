
using System.Text;


namespace Swish.Sftp.Packets
{
    public class ServiceRequest : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_SERVICE_REQUEST; } }

        public string ServiceName { get; set; }


        public override void Load(ByteReader reader)
        {
            ServiceName = reader.GetString(Encoding.UTF8);
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Server should never send a SSH_MSG_SERVICE_REQUEST message");
        }
    }
}
