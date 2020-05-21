
namespace Swish.Sftp
{
    public interface ISubsystem
    {
        string Name { get; }
        void HandleData(byte[] data);
    }
}
