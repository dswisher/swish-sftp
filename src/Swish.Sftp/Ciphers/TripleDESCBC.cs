
using System.Security.Cryptography;


namespace Swish.Sftp.Ciphers
{
    public class TripleDESCBC : ICipher
    {
        private readonly TripleDES des = TripleDES.Create();
        private ICryptoTransform encryptor;
        private ICryptoTransform decryptor;

        public TripleDESCBC()
        {
            des.KeySize = 192;
            des.Padding = PaddingMode.None;
            des.Mode = CipherMode.CBC;
        }

        public string Name { get { return "3des-cbc"; } }

        // TripleDES.BlockSize is the size of the block in bits, so we need to divide by 8
        // to convert from bits to bytes.
        public uint BlockSize { get { return (uint)(des.BlockSize / 8); } }

        // TripleDES.KeySize is the size of the block in bits, so we need to divide by 8
        // to convert from bits to bytes.
        public uint KeySize { get { return (uint)(des.KeySize / 8); } }


        public byte[] Decrypt(byte[] data)
        {
            return PerformTransform(decryptor, data);
        }


        public byte[] Encrypt(byte[] data)
        {
            return PerformTransform(encryptor, data);
        }


        public void SetKey(byte[] key, byte[] iv)
        {
            des.Key = key;
            des.IV = iv;

            decryptor = des.CreateDecryptor(key, iv);
            encryptor = des.CreateEncryptor(key, iv);
        }


        private byte[] PerformTransform(ICryptoTransform transform, byte[] data)
        {
            if (transform == null)
            {
                throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, "SetKey must be called before attempting to encrypt or decrypt data.");
            }

            var output = new byte[data.Length];
            transform.TransformBlock(data, 0, data.Length, output, 0);

            return output;
        }
    }
}
