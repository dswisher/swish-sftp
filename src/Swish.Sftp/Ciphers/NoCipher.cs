
namespace Swish.Sftp.Ciphers
{
    public class NoCipher : ICipher
    {
        public string Name { get { return "none"; } }

        public uint BlockSize { get { return 8; } }
        public uint KeySize { get { return 0; } }

        public byte[] Decrypt(byte[] data)
        {
            return data;
        }

        public byte[] Encrypt(byte[] data)
        {
            return data;
        }

        public void SetKey(byte[] key, byte[] iv)
        {
        }
    }
}
