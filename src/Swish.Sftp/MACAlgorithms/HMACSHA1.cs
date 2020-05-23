
namespace Swish.Sftp.MACAlgorithms
{
    public class HMACSHA1 : IMACAlgorithm
    {
        private System.Security.Cryptography.HMACSHA1 hmac;

        public string Name { get { return "hmac-sha1"; } }

        // See https://tools.ietf.org/html/rfc4253#section-6.4
        public uint KeySize { get { return 20; } }

        // See https://tools.ietf.org/html/rfc4253#section-6.4
        public uint DigestLength { get { return 20; } }


        public byte[] ComputeHash(uint packetNumber, byte[] data)
        {
            if (hmac == null)
            {
                throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_KEY_EXCHANGE_FAILED, "SetKey must be called before attempting to ComputeHash");
            }

            using (ByteWriter writer = new ByteWriter())
            {
                writer.WriteUInt32(packetNumber);
                writer.WriteRawBytes(data);
                return hmac.ComputeHash(writer.ToByteArray());
            }
        }


        public void SetKey(byte[] key)
        {
            hmac = new System.Security.Cryptography.HMACSHA1(key);
        }
    }
}
