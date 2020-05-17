
using Swish.Sftp.Ciphers;


namespace Swish.Sftp
{
    public class ExchangeContext
    {
        // TODO - public IKexAlgorithm KexAlgorithm { get; set; } = null;
        // TODO - public IHostKeyAlgorithm HostKeyAlgorithm { get; set; } = null;

        public ICipher CipherClientToServer { get; set; } = new NoCipher();
        public ICipher CipherServerToClient { get; set; } = new NoCipher();

        // TODO - public IMACAlgorithm MACAlgorithmClientToServer { get; set; } = null;
        // TODO - public IMACAlgorithm MACAlgorithmServerToClient { get; set; } = null;
        // TODO - public ICompression CompressionClientToServer { get; set; } = new NoCompression();
        // TODO - public ICompression CompressionServerToClient { get; set; } = new NoCompression();
    }
}
