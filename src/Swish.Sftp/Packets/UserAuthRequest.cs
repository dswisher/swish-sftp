
using System.Text;


namespace Swish.Sftp.Packets
{
    // See https://tools.ietf.org/html/rfc4252#section-5
    public class UserAuthRequest : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_USERAUTH_REQUEST; } }

        public string UserName { get; set; }
        public string ServiceName { get; set; }
        public string MethodName { get; set; }

        public bool ChangePassword { get; set; }
        public string Password { get; set; }    // only for password auth


        public override void Load(ByteReader reader)
        {
            UserName = reader.GetString(Encoding.UTF8);
            ServiceName = reader.GetString(Encoding.ASCII);
            MethodName = reader.GetString(Encoding.ASCII);

            if (MethodName == "password")
            {
                ChangePassword = reader.GetBoolean();
                Password = reader.GetString(Encoding.UTF8);
            }
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Server should never send a SSH_MSG_USERAUTH_REQUEST message");
        }
    }
}
