
using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Swish.Sftp.Subsystems.Sftp.Packets;


namespace Swish.Sftp.Subsystems.Sftp
{
    public class SftpSubsystem : ISubsystem
    {
        private readonly Dictionary<SftpPacketType, Type> packetTypes = new Dictionary<SftpPacketType, Type>();

        private readonly Channel channel;
        private readonly ILogger logger;

        private readonly IVirtualFileSystem fileSystem;


        public SftpSubsystem(Channel channel, IConfiguration config, ILogger logger)
        {
            this.channel = channel;
            this.logger = logger;

            packetTypes.Add(SftpPacketType.SSH_FXP_INIT, typeof(InitPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_REALPATH, typeof(RealPathPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_OPENDIR, typeof(OpenDirPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_READDIR, typeof(ReadDirPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_CLOSE, typeof(ClosePacket));

            // TODO - need a way to local the file system specific to a given user
            // TODO - need a way to specify different file systems - like S3 and whatnot
            fileSystem = new SimpleFileSystem(config, logger);
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

            var name = new NamePacket
            {
                Id = packet.Id
            };

            // TODO - properly set Longname - need that from file system, too.
            var realPath = fileSystem.GetRealPath(packet.Path);

            name.AddEntry(new NamePacket.Entry
            {
                Filename = realPath,
                Longname = realPath     // TODO - fix!
            });

            Send(name);
        }


        private void HandlePacket(OpenDirPacket packet)
        {
            logger.LogDebug("Processing OpenDir SFTP packet, Id={Id}, Path='{Path}'.", packet.Id, packet.Path);

            // TODO - respond with SSH_FXP_HANDLE (or SSH_FXP_STATUS if no read permission)
            // TODO - handle should be its own class, and we need to keep a dictionary/list of them
            var handle = new HandlePacket
            {
                Id = packet.Id,
                Handle = "fred"
            };

            Send(handle);
        }


        private void HandlePacket(ReadDirPacket packet)
        {
            logger.LogDebug("Processing ReadDir SFTP packet, Id={Id}, Handle='{Handle}'.", packet.Id, packet.Handle);

            // TODO - send another batch of files/dirs, or EOF if no more
            // TODO - keep the batch small enough that we do not exceed the connection's max packet size
            var status = new StatusPacket
            {
                Id = packet.Id,
                StatusCode = 1,     // EOF - TODO - need enum!
                ErrorMessage = "no more files"
            };

            Send(status);
        }


        private void HandlePacket(ClosePacket packet)
        {
            logger.LogDebug("Processing Close SFTP packet, Id={Id}, Handle='{Handle}'.", packet.Id, packet.Handle);

            // TODO - send back the proper status - should be done by the Handle class!
            var status = new StatusPacket
            {
                Id = packet.Id,
                StatusCode = 0,     // OK - TODO - need enum!
                ErrorMessage = "closed"
            };

            Send(status);
        }
    }
}
