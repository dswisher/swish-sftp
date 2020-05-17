
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Swish.Sftp.Packets;


namespace Swish.Sftp
{
    public class ByteWriter : IDisposable
    {
        private bool hasBeenDisposed;
        private MemoryStream stream = new MemoryStream();


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


        public void WriteBytes(byte[] data)
        {
            WriteUInt32((uint)data.Count());
            WriteRawBytes(data);
        }


        public void WriteString(string data)
        {
            WriteString(data, Encoding.ASCII);
        }


        public void WriteString(string data, Encoding encoding)
        {
            WriteBytes(encoding.GetBytes(data));
        }


        public void WriteStringList(IEnumerable<string> list)
        {
            WriteString(string.Join(",", list));
        }


        public void WriteMPInt(byte[] value)
        {
            if ((value.Length == 1) && (value[0] == 0))
            {
                WriteUInt32(0);
                return;
            }

            uint length = (uint)value.Length;
            if ((value[0] & 0x80) != 0)
            {
                WriteUInt32((uint)length + 1);
                WriteByte(0x00);
            }
            else
            {
                WriteUInt32((uint)length);
            }

            WriteRawBytes(value);
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
