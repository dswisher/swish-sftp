
namespace Swish.Sftp.HostKeyAlgorithms
{
    public interface IHostKeyAlgorithm : IAlgorithm
    {
        void ImportKeyFromFile(string path);
        byte[] CreateKeyAndCertificatesData();
        byte[] CreateSignatureData(byte[] hash);
    }
}
