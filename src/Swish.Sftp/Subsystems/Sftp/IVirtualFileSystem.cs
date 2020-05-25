
namespace Swish.Sftp.Subsystems.Sftp
{
    // TODO - move this to a more "common" place so scp could use it?
    public interface IVirtualFileSystem
    {
        string GetRealPath(string path);
    }
}
