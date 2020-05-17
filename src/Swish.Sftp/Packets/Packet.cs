
namespace Swish.Sftp.Packets
{
    public abstract class Packet
    {
        public const int MaxPacketSize = 35000;
        public const int PacketHeaderSize = 5;

        public abstract PacketType PacketType { get; }

        public uint PacketSequence { get; set; }


        public byte[] GetBytes()
        {
            using (var writer = new ByteWriter())
            {
                writer.WritePacketType(PacketType);
                InternalGetBytes(writer);
                return writer.ToByteArray();
            }
        }


        protected abstract void InternalGetBytes(ByteWriter writer);
    }
}
