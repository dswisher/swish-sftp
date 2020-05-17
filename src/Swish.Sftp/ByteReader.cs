
using System;
using System.IO;
using System.Linq;


namespace Swish.Sftp
{
    public class ByteReader : IDisposable
    {
        private bool hasBeenDisposed;
        private MemoryStream stream;


        public ByteReader(byte[] data)
        {
            stream = new MemoryStream(data);
        }


        public byte GetByte()
        {
            if (hasBeenDisposed)
            {
                throw new ObjectDisposedException("ByteReader");
            }

            return (byte)stream.ReadByte();
        }


        public byte[] GetBytes(int length)
        {
            if (hasBeenDisposed)
            {
                throw new ObjectDisposedException("ByteReader");
            }

            var data = new byte[length];
            stream.Read(data, 0, length);

            return data;
        }


        public uint GetUInt32()
        {
            var data = GetBytes(4);

            if (BitConverter.IsLittleEndian)
            {
                data = data.Reverse().ToArray();
            }

            return BitConverter.ToUInt32(data, 0);
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
