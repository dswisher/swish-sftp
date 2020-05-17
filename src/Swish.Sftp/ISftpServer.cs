
using System.Threading;
using System.Threading.Tasks;

namespace Swish.Sftp
{
    public interface ISftpServer
    {
        Task Run(CancellationToken cancellationToken);
    }
}
