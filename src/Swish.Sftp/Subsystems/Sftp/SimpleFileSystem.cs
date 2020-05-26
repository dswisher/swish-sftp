
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Swish.Sftp.Subsystems.Sftp
{
    public class SimpleFileSystem : IVirtualFileSystem
    {
        private readonly ILogger logger;

        private readonly DirectoryInfo rootDir;

        private string currentDir;


        public SimpleFileSystem(IConfiguration config, ILogger logger)
        {
            this.logger = logger;

            currentDir = "/";

            var rootPath = config.GetSection("vfs").GetValue<string>("rootPath");
            rootDir = new DirectoryInfo(rootPath);

            // TODO - HACK!
            // var files = string.Join(", ", rootDir.GetFiles().Select(x => x.Name));
            // throw new Exception($"RootDir: {rootDir.FullName}, files: {files}");
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


        public bool CanReadDirectory(string path)
        {
            var info = GetRealDir(path);

            return (info != null) && info.Exists;
        }


        public IEnumerable<VirtualFileItem> GetFilesInDirectory(string path)
        {
            var info = GetRealDir(path);

            // If the info is null, the directory does not exist, and we really shouldn't have gotten
            // to this point, but check nonetheless.
            if (info != null)
            {
                foreach (var file in info.GetFileSystemInfos())
                {
                    yield return new VirtualFileItem
                    {
                        Name = file.Name
                    };
                }
            }
        }


        private DirectoryInfo GetRealDir(string path)
        {
            // Try to keep them inside the directory tree
            if (path.Contains(".."))
            {
                return null;
            }

            var realPath = Path.Join(rootDir.FullName, path);
            var info = new DirectoryInfo(realPath);

            return info;
        }
    }
}
