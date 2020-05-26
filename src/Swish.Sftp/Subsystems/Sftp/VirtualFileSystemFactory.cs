
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Swish.Sftp.Subsystems.Sftp
{
    public class VirtualFileSystemFactory : IVirtualFileSystemFactory
    {
        private readonly IConfiguration config;
        private readonly ILoggerFactory loggerFactory;


        public VirtualFileSystemFactory(IConfiguration config,
                                        ILoggerFactory loggerFactory)
        {
            this.config = config;
            this.loggerFactory = loggerFactory;
        }


        public IVirtualFileSystem Create()
        {
            var logger = loggerFactory.CreateLogger<SimpleFileSystem>();

            // TODO - need a way to local the file system specific to a given user
            // TODO - need a way to specify different file systems - like S3 and whatnot
            return new SimpleFileSystem(config, logger);
        }
    }
}
