
namespace Swish.Sftp.Packets
{
    public class KexDHInit : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_KEXDH_INIT; } }
        public byte[] ClientValue { get; private set; }


        public override void Load(ByteReader reader)
        {
            ClientValue = reader.GetMPInt();
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            // Server never sends this
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, "Server should never send a SSH_MSG_KEXDH_INIT message");
        }
    }
}
