
using Swish.Sftp.Subsystems.Sftp.Packets;


namespace Swish.Sftp.Subsystems.Sftp
{
    public interface ISftpSubsystem : ISubsystem
    {
         void Send(SftpPacket packet);
    }
}
