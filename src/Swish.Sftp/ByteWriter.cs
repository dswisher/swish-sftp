
using System;
using System.IO;
using System.Linq;

using Swish.Sftp.Packets;


namespace Swish.Sftp
{
    public class ByteWriter : IDisposable
    {
        private bool hasBeenDisposed;
        private MemoryStream stream;


        public void WritePacketType(PacketType packetType)
        {
            WriteByte((byte)packetType);
        }


        public void WriteByte(byte value)
        {
            if (hasBeenDisposed)
            {
                throw new ObjectDisposedException("ByteWriter");
            }

            stream.WriteByte(value);
        }


        public void WriteUInt32(uint data)
        {
            byte[] buffer = BitConverter.GetBytes(data);

            if (BitConverter.IsLittleEndian)
            {
                buffer = buffer.Reverse().ToArray();
            }

            WriteRawBytes(buffer);
        }


        public void WriteRawBytes(byte[] value)
        {
            if (hasBeenDisposed)
            {
                throw new ObjectDisposedException("ByteWriter");
            }

            stream.Write(value, 0, value.Length);
        }


        public byte[] ToByteArray()
        {
            if (hasBeenDisposed)
            {
                throw new ObjectDisposedException("ByteWriter");
            }

            return stream.ToArray();
        }


        public void Dispose()
        {
            Dispose(true);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!hasBeenDisposed)
            {
                if (disposing)
                {
                    stream.Dispose();
                    stream = null;
                }

                hasBeenDisposed = true;
            }
        }
    }
}
