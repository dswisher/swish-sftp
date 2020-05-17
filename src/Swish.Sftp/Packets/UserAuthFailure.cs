
using System.Collections.Generic;


namespace Swish.Sftp.Packets
{
    // See https://tools.ietf.org/html/rfc4252#section-5.1
    public class UserAuthFailure : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_USERAUTH_FAILURE; } }

        public List<string> AuthTypesThatCanContinue { get; private set; } = new List<string>();
        public bool PartialSuccess { get; set; }


        public override void Load(ByteReader reader)
        {
            // Client never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_MSG_USERAUTH_FAILURE message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteStringList(AuthTypesThatCanContinue);
            writer.WriteBool(PartialSuccess);
        }
    }
}
