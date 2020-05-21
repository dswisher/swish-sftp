
namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public abstract class SftpPacket
    {
        public abstract SftpPacketType PacketType { get; }


        public byte[] GetBytes()
        {
            // First, build the inner bytes
            byte[] payload;

            using (var writer = new ByteWriter())
            {
                writer.WriteByte((byte)PacketType);
                InternalGetBytes(writer);

                payload = writer.ToByteArray();
            }

            // Now, package it all up
            using (var writer = new ByteWriter())
            {
                writer.WriteUInt32((uint)payload.Length);
                writer.WriteRawBytes(payload);

                return writer.ToByteArray();
            }
        }


        public abstract void Load(ByteReader reader);
        protected abstract void InternalGetBytes(ByteWriter writer);
    }
}
