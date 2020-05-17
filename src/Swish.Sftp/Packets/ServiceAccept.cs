
using System.Text;


namespace Swish.Sftp.Packets
{
    public class ServiceAccept : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_SERVICE_ACCEPT; } }

        public string ServiceName { get; set; }


        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_MSG_SERVICE_REQUEST message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteString(ServiceName, Encoding.UTF8);
        }
    }
}
