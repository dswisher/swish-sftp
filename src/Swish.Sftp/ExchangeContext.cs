
using Swish.Sftp.Ciphers;
using Swish.Sftp.Compressions;
using Swish.Sftp.HostKeyAlgorithms;
using Swish.Sftp.KexAlgorithms;
using Swish.Sftp.MACAlgorithms;

namespace Swish.Sftp
{
    public class ExchangeContext
    {
        public IKexAlgorithm KexAlgorithm { get; set; } = null;
        public IHostKeyAlgorithm HostKeyAlgorithm { get; set; } = null;
        public ICipher CipherClientToServer { get; set; } = new NoCipher();
        public ICipher CipherServerToClient { get; set; } = new NoCipher();
        public IMACAlgorithm MACAlgorithmClientToServer { get; set; } = null;
        public IMACAlgorithm MACAlgorithmServerToClient { get; set; } = null;
        public ICompression CompressionClientToServer { get; set; } = new NoCompression();
        public ICompression CompressionServerToClient { get; set; } = new NoCompression();
    }
}
