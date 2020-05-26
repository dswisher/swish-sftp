
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Swish.Sftp.Subsystems.Sftp.Packets;


namespace Swish.Sftp.Subsystems.Sftp
{
    public class SftpHandle
    {
        private readonly ISftpSubsystem sftp;
        private readonly IVirtualFileSystem fileSystem;
        private readonly ILogger logger;

        private Queue<VirtualFileItem> pendingDirectoryItems;


        public SftpHandle(ISftpSubsystem sftp, string name, IVirtualFileSystem fileSystem, ILogger logger)
        {
            this.sftp = sftp;
            this.fileSystem = fileSystem;
            this.logger = logger;

            Name = name;
        }


        public string Name { get; private set; }


        public void OpenDir(string path)
        {
            var items = fileSystem.GetFilesInDirectory(path);

            pendingDirectoryItems = new Queue<VirtualFileItem>();

            foreach (var item in items)
            {
                pendingDirectoryItems.Enqueue(item);
            }
        }


        public void ReadDir(uint requestId)
        {
            // If the pending items list is null, OpenDir was never called
            if (pendingDirectoryItems == null)
            {
                var packet = new StatusPacket
                {
                    Id = requestId,
                    StatusCode = StatusPacket.Failure,
                    ErrorMessage = "directory not open"
                };

                sftp.Send(packet);
            }

            // If the pending items list is empty, we're done - send EOF
            if (pendingDirectoryItems.Count == 0)
            {
                var packet = new StatusPacket
                {
                    Id = requestId,
                    StatusCode = StatusPacket.Eof,
                    ErrorMessage = "no more files"
                };

                sftp.Send(packet);
            }
            else
            {
                // We have files to send...send a batch.
                // TODO - keep the batch small enough that we do not exceed the connection's max packet size
                // TODO - implement me!
                var packet = new NamePacket();

                // TODO - parameterize the max entry count (and eventually base it off packet size)
                while ((pendingDirectoryItems.Count > 0) && (packet.Entries.Count < 10))
                {
                    packet.Entries.Add(pendingDirectoryItems.Dequeue().AsPacketEntry());
                }

                sftp.Send(packet);
            }
        }
    }
}
