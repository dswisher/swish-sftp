
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace Swish.Sftp
{
    public class ByteReader : IDisposable
    {
        private readonly char[] listSeparator = new char[] { ',' };

        private bool hasBeenDisposed;
        private MemoryStream stream;


        public ByteReader(byte[] data)
        {
            stream = new MemoryStream(data);
        }


        public bool IsEOF
        {
            get
            {
                if (hasBeenDisposed)
                {
                    throw new ObjectDisposedException("ByteReader");
                }

                return stream.Position == stream.Length;
            }
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


        public string GetString()
        {
            return GetString(Encoding.ASCII);
        }


        public string GetString(Encoding encoding)
        {
            int length = (int)GetUInt32();

            if (length == 0)
            {
                return string.Empty;
            }

            return encoding.GetString(GetBytes(length));
        }


        public List<string> GetNameList()
        {
            return new List<string>(GetString().Split(listSeparator, StringSplitOptions.RemoveEmptyEntries));
        }


        public bool GetBoolean()
        {
            return GetByte() != 0;
        }


        public byte[] GetMPInt()
        {
            uint size = GetUInt32();

            if (size == 0)
            {
                return new byte[1];
            }

            byte[] data = GetBytes((int)size);

            if (data[0] == 0)
            {
                return data.Skip(1).ToArray();
            }

            return data;
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
