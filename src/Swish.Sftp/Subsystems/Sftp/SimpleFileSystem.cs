
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Swish.Sftp.Subsystems.Sftp
{
    public class SimpleFileSystem : IVirtualFileSystem
    {
        private readonly ILogger logger;

        private readonly string currentDir;


        public SimpleFileSystem(IConfiguration config, ILogger logger)
        {
            this.logger = logger;

            currentDir = "/";

            // TODO - read the config to find the root
        }


        public string GetRealPath(string path)
        {
            if (path == ".")
            {
                return currentDir;
            }
            else
            {
                // TODO - implement me!
                logger.LogWarning("Don't know how to get the RealPath for '{Path}'.", path);
                return "/";
            }
        }
    }
}
