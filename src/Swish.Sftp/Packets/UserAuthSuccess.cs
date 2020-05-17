
namespace Swish.Sftp.Packets
{
    // See https://tools.ietf.org/html/rfc4252#section-5.1
    public class UserAuthSuccess : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_USERAUTH_SUCCESS; } }


        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_MSG_USERAUTH_FAILURE message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // No data
        }
    }
}
