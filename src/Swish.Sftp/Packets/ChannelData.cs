

namespace Swish.Sftp.Packets
{
    public class ChannelData : Packet
    {
        public override PacketType PacketType { get { return PacketType.SSH_MSG_CHANNEL_DATA; } }

        public uint RecipientChannel { get; set; }

        public byte[] Data { get; set; }

        /*
        // public string Data { get; set; }

        // TODO - HACK - RFC 4254 specifies that the channel data is a string. But it looks like SFTP treats the data as binary...

        public uint Length { get; set; }
        public uint FxpLength { get; set; }
        public byte Type { get; set; }
        public byte[] RawData { get; set; }

        public uint Version { get; set; }
        */


        public override void Load(ByteReader reader)
        {
            RecipientChannel = reader.GetUInt32();

            var length = (int)reader.GetUInt32();

            Data = reader.GetBytes(length);

            /*
            Length = reader.GetUInt32();    // Length of the data area
            FxpLength = reader.GetUInt32(); // Length of the SFTP sub-area
            Type = reader.GetByte();

            // TODO - clean up this mess

            RawData = reader.GetBytes((int)(FxpLength - 1));      // -1 as we've already read the Type

            using (var subreader = new ByteReader(RawData))
            {
                // TODO - enum - SSH_FXP_INIT
                if (Type == 1)
                {
                    Version = subreader.GetUInt32();

                    // TODO - extension data?
                }
            }
            */
        }


        protected override void InternalGetBytes(ByteWriter writer)
        {
            writer.WriteUInt32(RecipientChannel);
            writer.WriteBytes(Data);

            /*
            writer.WriteUInt32(Length);
            writer.WriteUInt32(FxpLength);
            writer.WriteByte(Type);

            // TODO - enum - SSH_FXP_VERSION
            if (Type == 2)
            {
                writer.WriteUInt32(Version);
            }
            */
        }
    }
}
