
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace Swish.Sftp.HostKeyAlgorithms
{
    public class SSHRSA : IHostKeyAlgorithm
    {
        private readonly RSA rsa = RSA.Create();

        public string Name { get { return "ssh-rsa"; } }

        public byte[] CreateKeyAndCertificatesData()
        {
            RSAParameters param = rsa.ExportParameters(false);

            using (ByteWriter writer = new ByteWriter())
            {
                writer.WriteString(Name);
                writer.WriteMPInt(param.Exponent);
                writer.WriteMPInt(param.Modulus);
                return writer.ToByteArray();
            }
        }

        public byte[] CreateSignatureData(byte[] hash)
        {
            using (ByteWriter writer = new ByteWriter())
            {
                writer.WriteString(Name);
                writer.WriteBytes(rsa.SignData(hash, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
                return writer.ToByteArray();
            }
        }


        public void ImportKeyFromFile(string path)
        {
            var builder = new StringBuilder();
            using (var reader = new StreamReader(path))
            {
                var first = true;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("-----", StringComparison.OrdinalIgnoreCase))
                    {
                        if (first)
                        {
                            // TODO - check for proper string
                            first = false;
                        }
                    }
                    else
                    {
                        builder.Append(line);
                    }
                }
            }

            var privateKeyBytes = Convert.FromBase64String(builder.ToString());

            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        }
    }
}
