
using Swish.Sftp.Subsystems.Sftp.Packets;

namespace Swish.Sftp.Subsystems.Sftp
{
    public static class Extensions
    {
        public static NamePacket.Entry AsPacketEntry(this VirtualFileItem item)
        {
            var entry = new NamePacket.Entry
            {
                // TODO - populate other fields
                Filename = item.Name,
                Longname = item.Name    // TODO - wrong!
            };

            return entry;
        }
    }
}
