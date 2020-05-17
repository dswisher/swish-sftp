
namespace Swish.Sftp.Packets
{
    public class KexDHReply : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_KEXDH_REPLY; } }

        public byte[] ServerHostKey { get; set; }
        public byte[] ServerValue { get; set; }
        public byte[] Signature { get; set; }


        public override void Load(ByteReader reader)
        {
            // Client never sends this!
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, "Client should never send a SSH_MSG_KEXDH_REPLY message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteBytes(ServerHostKey);
            writer.WriteMPInt(ServerValue);
            writer.WriteBytes(Signature);
        }
    }
}
