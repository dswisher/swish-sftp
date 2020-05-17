
namespace Swish.Sftp.Compressions
{
    public class NoCompression : ICompression
    {
        public byte[] Compress(byte[] data)
        {
            return data;
        }


        public byte[] Decompress(byte[] data)
        {
            return data;
        }
    }
}
