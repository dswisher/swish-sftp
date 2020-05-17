
namespace Swish.Sftp.HostKeyAlgorithms
{
    public interface IHostKeyAlgorithm : IAlgorithm
    {
        void ImportKey(string keyXml);
        byte[] CreateKeyAndCertificatesData();
        byte[] CreateSignatureData(byte[] hash);
    }
}
