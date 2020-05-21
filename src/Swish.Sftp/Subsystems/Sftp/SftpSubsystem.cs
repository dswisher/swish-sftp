
using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Swish.Sftp.Subsystems.Sftp.Packets;


namespace Swish.Sftp.Subsystems.Sftp
{
    public class SftpSubsystem : ISubsystem
    {
        private readonly Dictionary<SftpPacketType, Type> packetTypes = new Dictionary<SftpPacketType, Type>();

        private readonly Channel channel;
        private readonly ILogger logger;

        public SftpSubsystem(Channel channel, ILogger logger)
        {
            this.channel = channel;
            this.logger = logger;

            packetTypes.Add(SftpPacketType.SSH_FXP_INIT, typeof(InitPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_REALPATH, typeof(RealPathPacket));
        }


        public string Name { get { return "sftp"; } }


        public void HandleData(byte[] data)
        {
            using (var reader = new ByteReader(data))
            {
                var length = reader.GetUInt32() - 1;    // -1 because we'll read the type next
                var type = (SftpPacketType)reader.GetByte();

                if (packetTypes.ContainsKey(type))
                {
                    var packet = Activator.CreateInstance(packetTypes[type]) as SftpPacket;
                    packet.Load(reader);

                    try
                    {
                        HandlePacket((dynamic)packet);
                    }
                    catch
                    {
                        logger.LogWarning("Unhandled SFTP packet type: {Type}.", type);
                    }
                }
                else
                {
                    logger.LogWarning("Unimplemented SFTP packet type: {Type}.", type);
                }
            }
        }


        private void Send(SftpPacket packet)
        {
            logger.LogDebug("Sending SFTP {Type} packet.", packet.PacketType);

            // TODO - too much copying of bytes down inside here!
            channel.SendData(packet.GetBytes());
        }


        private void HandlePacket(InitPacket packet)
        {
            logger.LogDebug("Processing Init SFTP packet, version={Version}.", packet.Version);

            var version = new VersionPacket
            {
                // Version = Math.Min(packet.Version, 3)
                Version = 3
            };

            Send(version);
        }


        private void HandlePacket(RealPathPacket packet)
        {
            logger.LogDebug("Processing RealPath SFTP packet, Id={Id}, Path='{Path}'.", packet.Id, packet.Path);

            // TODO - respond with an SSH_FXP_NAME packet
        }
    }
}
