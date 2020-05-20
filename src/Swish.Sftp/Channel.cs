
using Microsoft.Extensions.Logging;
using Swish.Sftp.Packets;


namespace Swish.Sftp
{
    public class Channel
    {
        private readonly IPacketSender packetSender;
        private readonly ILogger logger;

        public Channel(IPacketSender packetSender, ILogger logger, uint serverChannelId, ChannelOpen packet)
        {
            this.packetSender = packetSender;
            this.logger = logger;

            ServerChannelId = serverChannelId;
            ClientChannelId = packet.SenderChannel;
            WindowSize = packet.InitialWindowSize;
            MaximumPacketSize = packet.MaximumPacketSize;

            var success = new ChannelOpenConfirmation
            {
                RecipientChannel = ClientChannelId,
                SenderChannel = ServerChannelId,
                InitialWindowSize = WindowSize,
                MaximumPacketSize = MaximumPacketSize
            };

            packetSender.Send(success);
        }


        public uint ServerChannelId { get; set; }
        public uint ClientChannelId { get; set; }
        public uint WindowSize { get; set; }
        public uint MaximumPacketSize { get; set; }


        public void HandlePacket(ChannelRequest packet)
        {
            if (packet.RequestType == "env")
            {
                logger.LogDebug("   -> {Name} = '{Value}'.", packet.VariableName, packet.VariableValue);
            }
            else if (packet.RequestType == "subsystem")
            {
                logger.LogDebug("   -> subsystem name = '{Name}'.", packet.SubsystemName);

                if (packet.WantReply)
                {
                    var success = new ChannelSuccess
                    {
                        RecipientChannel = ClientChannelId
                    };

                    packetSender.Send(success);
                }
            }

            // TODO - log a warning for unknown type?
        }


        public void HandlePacket(ChannelData packet)
        {
            // TODO - SSH_FXP_INIT
            if (packet.Type == 1)
            {
                logger.LogDebug("   -> version {Version}", packet.Version);

                // TODO - clean up! This is horrible, but trying to get something working!
                var data = new ChannelData
                {
                    RecipientChannel = ClientChannelId,
                    Length = 9,
                    FxpLength = 5,
                    Type = 2,
                    Version = 3
                };

                packetSender.Send(data);
            }
        }
    }
}
