
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Swish.Sftp.Packets;
using Swish.Sftp.Subsystems.Sftp;


namespace Swish.Sftp
{
    public class Channel
    {
        private readonly IPacketSender packetSender;
        private readonly ILogger logger;

        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();

        private ISubsystem subsystem;


        public Channel(IPacketSender packetSender, ILogger logger, uint serverChannelId, ChannelOpen packet)
        {
            this.packetSender = packetSender;
            this.logger = logger;

            ServerChannelId = serverChannelId;
            ClientChannelId = packet.SenderChannel;
            WindowSize = packet.InitialWindowSize;
            MaximumPacketSize = packet.MaximumPacketSize;
        }


        public uint ServerChannelId { get; set; }
        public uint ClientChannelId { get; set; }
        public uint WindowSize { get; set; }
        public uint MaximumPacketSize { get; set; }

        public string SubsystemName
        {
            get
            {
                return subsystem?.Name;
            }
        }


        public void Init()
        {
            var success = new ChannelOpenConfirmation
            {
                RecipientChannel = ClientChannelId,
                SenderChannel = ServerChannelId,
                InitialWindowSize = WindowSize,
                MaximumPacketSize = MaximumPacketSize
            };

            packetSender.Send(success);
        }


        public void HandlePacket(ChannelRequest packet)
        {
            // TODO - refactor ChannelRequest so it just has a data area that the channel parses. The packet should not have every possible property.

            var ok = false;

            if (packet.RequestType == "env")
            {
                logger.LogDebug("   -> {Name} = '{Value}'.", packet.VariableName, packet.VariableValue);

                // TODO - apply some sanity checks to name and value
                variables.Add(packet.VariableName, packet.VariableValue);
            }
            else if (packet.RequestType == "subsystem")
            {
                logger.LogDebug("   -> subsystem name = '{Name}'.", packet.SubsystemName);

                if (packet.SubsystemName == "sftp")
                {
                    // TODO - use a factory to create the subsystem, so we can pass in a logger and whatnot
                    subsystem = new SftpSubsystem(this, logger);

                    ok = true;
                }
            }

            // TODO - log a warning for unknown type?

            if (packet.WantReply)
            {
                if (ok)
                {
                    var success = new ChannelSuccess
                    {
                        RecipientChannel = ClientChannelId
                    };

                    packetSender.Send(success);
                }
                else
                {
                    var failure = new ChannelFailure
                    {
                        RecipientChannel = ClientChannelId
                    };

                    packetSender.Send(failure);
                }
            }
        }


        public void HandlePacket(ChannelData packet)
        {
            // TODO - if we have a subsystem, send the data there

            // If we have a subsystem, let it deal with the data, otherwise ignore.
            if (subsystem != null)
            {
                subsystem.HandleData(packet.Data);
            }

            /*
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
            */
        }


        public void SendData(byte[] data)
        {
            var packet = new ChannelData
            {
                RecipientChannel = ClientChannelId,
                Data = data
            };

            packetSender.Send(packet);
        }


        public string GetEnvironmentVariable(string name)
        {
            if (variables.ContainsKey(name))
            {
                return variables[name];
            }

            return null;
        }
    }
}
