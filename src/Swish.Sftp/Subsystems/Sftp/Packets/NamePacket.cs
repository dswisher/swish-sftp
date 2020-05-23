
using System.Collections.Generic;


namespace Swish.Sftp.Subsystems.Sftp.Packets
{
    public class NamePacket : SftpPacket
    {
        private readonly List<Entry> items = new List<Entry>();

        public override SftpPacketType PacketType { get { return SftpPacketType.SSH_FXP_NAME; } }

        public uint Id { get; set; }
        public IList<Entry> Entries { get { return items; } }


        public void AddEntry(Entry entry)
        {
            items.Add(entry);
        }


        public override void Load(ByteReader reader)
        {
            // Client never sends this (so far as I know)
            throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Client should never send a SSH_FXP_NAME message");
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(Id);
            writer.WriteUInt32((uint)items.Count);

            foreach (var item in items)
            {
                writer.WriteString(item.Filename);
                writer.WriteString(item.Longname);

                // TODO - ATTRS
                writer.WriteUInt32(0);  // TODO - ATTR hack!
            }
        }


        public class Entry
        {
            public string Filename { get; set; }
            public string Longname { get; set; }

            // TODO - ATTRS
        }
    }
}
