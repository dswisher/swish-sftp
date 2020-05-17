
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Swish.Sftp.Packets;


namespace Swish.Sftp
{
    public class Client
    {
        private static long nextClientId;

        private readonly Socket socket;
        private readonly ILogger logger;

        private ExchangeContext activeExchangeContext = new ExchangeContext();

        private int currentSentPacketNumber = -1;
        private int currentReceivedPacketNumber = -1;

        private bool protocolVersionExchangeComplete;
        private string protocolVersionExchange;
        private long totalBytesTransferred;


        public Client(Socket socket, ILogger<Client> logger)
        {
            this.socket = socket;
            this.logger = logger;

            Id = Interlocked.Increment(ref nextClientId).ToString();
            IsConnected = true;

            Send("SSH-2.0-swishsftp_0.0.1\r\n");
        }


        public string Id { get; private set; }
        public bool IsConnected { get; set; }


        public async Task PollAsync(CancellationToken cancellationToken)
        {
            // If we're not connected, don't bother polling. This _should_ be removed from the client list shortly.
            if (!IsConnected)
            {
                return;
            }

            // Is there data ready for us to read from the socket?
            var dataAvailable = socket.Poll(0, SelectMode.SelectRead);

            if (dataAvailable)
            {
                var bytesAvailable = socket.Available;

                if (bytesAvailable < 1)
                {
                    Disconnect(DisconnectReason.SSH_DISCONNECT_CONNECTION_LOST, "The client disconnected.");
                    return;
                }

                if (!protocolVersionExchangeComplete)
                {
                    try
                    {
                        ReadProtocolVersionExchange();

                        if (protocolVersionExchangeComplete)
                        {
                            logger.LogDebug("Received ProtocolVersionExchange: '{VersionExchange}'.", protocolVersionExchange);
                            ValidateProtocolVersionExchange();

                            // TODO - send KexInit
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Exception during protocol version exchange.");
                        Disconnect(DisconnectReason.SSH_DISCONNECT_PROTOCOL_VERSION_NOT_SUPPORTED, "Failed to get the protocol version exchange.");
                        return;
                    }
                }

                if (protocolVersionExchangeComplete)
                {
                    try
                    {
                        // TODO - async? cancel token?
                        var packet = ReadPacket();

                        // TODO - will reading in a loop allow starvation of other clients?
                        while (packet != null)
                        {
                            logger.LogDebug("Received packet: {Type}.", packet.PacketType);

                            // TODO - handle the packet

                            // Read next packet (if any)
                            packet = ReadPacket();
                        }

                        // TODO - read and process a packet

                        // TODO - Consider re-exchanging keys
                    }
                    catch (SwishServerException ex)
                    {
                        logger.LogError(ex, "Exception reading packet.");
                        Disconnect(ex.Reason, ex.Message);
                        return;
                    }
                }
            }

            // TODO - implement poll!
            await Task.CompletedTask;
        }


        public void Disconnect(DisconnectReason reason, string message)
        {
            logger.LogDebug("{Id} disconnected - {Reason} - {Message}", Id, reason, message);

            if (IsConnected)
            {
                if (reason != DisconnectReason.None)
                {
                    // TODO - send disconnect message to client
                }

                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "{Id}: Exception shutting down socket.", Id);
                }

                IsConnected = false;
            }
        }


        // TODO - async? cancel token?
        private void Send(string message)
        {
            logger.LogDebug("Sending raw string: '{Content}'.", message.Trim());
            Send(Encoding.UTF8.GetBytes(message));
        }


        // TODO - async? cancel token?
        private void Send(byte[] message)
        {
            if (!IsConnected)
            {
                return;
            }

            totalBytesTransferred += message.Length;

            socket.Send(message);
        }


        // TODO - async? cancel token?
        private void Send(Packet packet)
        {
            logger.LogDebug("Sending {0} packet.", packet.PacketType);

            packet.PacketSequence = GetSentPacketNumber();

            var payload = packet.GetBytes();

            // TODO - Compress
            // payload = activeExchangeContext.CompressionServerToClient.Compress(payload);

            uint blockSize = activeExchangeContext.CipherServerToClient.BlockSize;

            byte paddingLength = (byte)((blockSize - (payload.Length + 5)) % blockSize);
            if (paddingLength < 4)
            {
                paddingLength += (byte)blockSize;
            }

            byte[] padding = new byte[paddingLength];

            // TODO - fill padding with random goodness
            // RandomNumberGenerator.Create().GetBytes(padding);

            uint packetLength = (uint)(payload.Length + paddingLength + 1);

            using (ByteWriter writer = new ByteWriter())
            {
                writer.WriteUInt32(packetLength);
                writer.WriteByte(paddingLength);
                writer.WriteRawBytes(payload);
                writer.WriteRawBytes(padding);

                payload = writer.ToByteArray();
            }

            // Encrypt
            var encryptedPayload = activeExchangeContext.CipherServerToClient.Encrypt(payload);

            // TODO - apply MAC

            Send(encryptedPayload);

            // TODO - Consider re-exchanging keys
        }


        // Read 1 byte from the socket until \r\n
        private void ReadProtocolVersionExchange()
        {
            NetworkStream stream = new NetworkStream(socket, false);
            string result = null;

            List<byte> data = new List<byte>();

            bool foundCR = false;
            int val = stream.ReadByte();

            while (val != -1)
            {
                if (foundCR && (val == '\n'))
                {
                    result = Encoding.UTF8.GetString(data.ToArray());
                    protocolVersionExchangeComplete = true;
                    break;
                }

                if (val == '\r')
                {
                    foundCR = true;
                }
                else
                {
                    foundCR = false;
                    data.Add((byte)val);
                }

                val = stream.ReadByte();
            }

            protocolVersionExchange += result;
        }


        private void ValidateProtocolVersionExchange()
        {
            // https://tools.ietf.org/html/rfc4253#section-4.2
            // SSH-protoversion-softwareversion SP comments

            string[] pveParts = protocolVersionExchange.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (pveParts.Length == 0)
            {
                throw new UnauthorizedAccessException("Invalid Protocol Version Exchange was received - No Data");
            }

            string[] versionParts = pveParts[0].Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (versionParts.Length < 3)
            {
                throw new UnauthorizedAccessException($"Invalid Protocol Version Exchange was received - Not enough dashes - {pveParts[0]}");
            }

            if (versionParts[1] != "2.0")
            {
                throw new UnauthorizedAccessException($"Invalid Protocol Version Exchange was received - Unsupported Version - {versionParts[1]}");
            }

            // If we get here, all is well!
        }


        private Packet ReadPacket()
        {
            if (!IsConnected)
            {
                return null;
            }

            uint blockSize = activeExchangeContext.CipherClientToServer.BlockSize;

            // We must have at least one block available to read
            if (socket.Available < blockSize)
            {
                return null;
            }

            // We have a block, read it
            byte[] firstBlock = new byte[blockSize];
            int bytesRead = socket.Receive(firstBlock);
            if (bytesRead != blockSize)
            {
                throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_CONNECTION_LOST, "Failed to read from socket.");
            }

            // Decrypt the block
            firstBlock = activeExchangeContext.CipherClientToServer.Decrypt(firstBlock);

            // Pick out the packet length and padding. See https://tools.ietf.org/html/rfc4253#section-6
            uint packetLength = 0;      // The length of the packet in bytes, not including 'mac' or the 'packet_length' field itself.
            uint paddingLength = 0;     // Length of 'random padding' (bytes).
            using (var reader = new ByteReader(firstBlock))
            {
                packetLength = reader.GetUInt32();

                if (packetLength > Packet.MaxPacketSize)
                {
                    throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, $"Client tried to send a packet bigger than MaxPacketSize ({Packet.MaxPacketSize} bytes): {packetLength} bytes");
                }

                paddingLength = reader.GetByte();
            }

            // Now that we know the packet length, read the rest of the payload
            uint bytesToRead = packetLength - blockSize + 4;

            // TODO - possible bug? What if the client has only written a block's worth of data?

            var restOfPacket = new byte[bytesToRead];
            bytesRead = socket.Receive(restOfPacket);
            if (bytesRead != bytesToRead)
            {
                throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_CONNECTION_LOST, "Failed to read from socket.");
            }

            restOfPacket = activeExchangeContext.CipherClientToServer.Decrypt(restOfPacket);

            uint payloadLength = packetLength - paddingLength - 1;
            byte[] fullPacket = firstBlock.Concat(restOfPacket).ToArray();

            // Keep track of the total bytes transferred
            totalBytesTransferred += fullPacket.Length;

            // Pick out the payload
            var payload = fullPacket.Skip(Packet.PacketHeaderSize).Take((int)payloadLength).ToArray();

            // Get the packet number, so we know the sequence
            uint packetNumber = GetReceivedPacketNumber();

            // Check the MAC, if we have one
            // TODO - check MAC

            // Decompress
            // TODO - decompress

            // Parse the packet
            using (var reader = new ByteReader(payload))
            {
                var type = (PacketType)reader.GetByte();

                /*
                if (Packet.PacketTypes.ContainsKey(type))
                {
                    // TODO
                }
                */

                logger.LogWarning("Unimplemented packet type: {Type}.", type);

                var unimplemented = new Unimplemented
                {
                    RejectedPacketNumber = packetNumber
                };

                Send(unimplemented);
            }

            return null;
        }


        private uint GetSentPacketNumber()
        {
            return (uint)Interlocked.Increment(ref currentSentPacketNumber);
        }


        private uint GetReceivedPacketNumber()
        {
            return (uint)Interlocked.Increment(ref currentReceivedPacketNumber);
        }
    }
}
