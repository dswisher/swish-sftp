
using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using Swish.Sftp.Subsystems.Sftp.Packets;


namespace Swish.Sftp.Subsystems.Sftp
{
    public class SftpSubsystem : ISftpSubsystem
    {
        private readonly Dictionary<SftpPacketType, Type> packetTypes = new Dictionary<SftpPacketType, Type>();

        private readonly Channel channel;
        private readonly ILogger logger;

        private readonly IVirtualFileSystem fileSystem;
        private readonly Dictionary<string, SftpHandle> handles = new Dictionary<string, SftpHandle>();

        private int nextChannelId = 1;


        public SftpSubsystem(Channel channel, IVirtualFileSystemFactory fileSystemFactory, ILogger logger)
        {
            this.channel = channel;
            this.logger = logger;

            packetTypes.Add(SftpPacketType.SSH_FXP_INIT, typeof(InitPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_REALPATH, typeof(RealPathPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_OPENDIR, typeof(OpenDirPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_READDIR, typeof(ReadDirPacket));
            packetTypes.Add(SftpPacketType.SSH_FXP_CLOSE, typeof(ClosePacket));

            fileSystem = fileSystemFactory.Create();
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
                    catch (RuntimeBinderException)
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


        public void Send(SftpPacket packet)
        {
            logger.LogDebug("Sending SFTP {Type} packet. {Details}", packet.PacketType, packet.Details());

            // TODO - too much copying of bytes down inside here!
            channel.SendData(packet.GetBytes());
        }


        private bool TryGetHandle(uint requestId, string name, out SftpHandle handle)
        {
            if (handles.ContainsKey(name))
            {
                handle = handles[name];
                return true;
            }
            else
            {
                handle = null;

                var status = new StatusPacket
                {
                    Id = requestId,
                    StatusCode = StatusPacket.Failure,
                    ErrorMessage = "unknown channel"
                };

                Send(status);

                return false;
            }
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

            // If they cannot read the directory, respond with a status packet.
            if (!fileSystem.CanReadDirectory(packet.Path))
            {
                var status = new StatusPacket
                {
                    Id = packet.Id,
                    StatusCode = StatusPacket.PermissionDenied,
                    ErrorMessage = "permission denied"
                };

                Send(status);
            }
            else
            {
                // Create a new channel
                // TODO - use a factory to create the handle, so it can get its own logger
                var handleId = Interlocked.Increment(ref nextChannelId).ToString();
                var handle = new SftpHandle(this, handleId, fileSystem, logger);

                handles.Add(handleId, handle);

                handle.OpenDir(packet.Path);

                var response = new HandlePacket
                {
                    Id = packet.Id,
                    Handle = handle.Name
                };

                Send(response);
            }
        }


        private void HandlePacket(ReadDirPacket packet)
        {
            logger.LogDebug("Processing ReadDir SFTP packet, Id={Id}, Handle='{Handle}'.", packet.Id, packet.Handle);

            if (TryGetHandle(packet.Id, packet.Handle, out SftpHandle handle))
            {
                handle.ReadDir(packet.Id);
            }
        }


        private void HandlePacket(ClosePacket packet)
        {
            logger.LogDebug("Processing Close SFTP packet, Id={Id}, Handle='{Handle}'.", packet.Id, packet.Handle);

            if (TryGetHandle(packet.Id, packet.Handle, out SftpHandle handle))
            {
                handles.Remove(packet.Handle);

                var status = new StatusPacket
                {
                    Id = packet.Id,
                    StatusCode = StatusPacket.Ok,
                    ErrorMessage = "closed"
                };

                Send(status);
            }
        }
    }
}
