
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using Swish.Sftp.KexAlgorithms;
using Swish.Sftp.Packets;


namespace Swish.Sftp
{
    public class Client
    {
        private const string ServerProtocolVersion = "SSH-2.0-swishsftp_0.0.1";

        private static long nextClientId;

        private readonly Socket socket;
        private readonly ILogger logger;

        private KexInit kexInitServerToClient = new KexInit();
        private KexInit kexInitClientToServer;

        private ExchangeContext activeExchangeContext = new ExchangeContext();
        private ExchangeContext pendingExchangeContext = new ExchangeContext();

        private int currentSentPacketNumber = -1;
        private int currentReceivedPacketNumber = -1;

        private bool protocolVersionExchangeComplete;
        private string protocolVersionExchange;
        private long totalBytesTransferred;
        private DateTime keyTimeout;

        private byte[] sessionId;


        public Client(Socket socket, ILogger<Client> logger)
        {
            this.socket = socket;
            this.logger = logger;

            Id = Interlocked.Increment(ref nextClientId).ToString();
            IsConnected = true;

            // Set up the socket
            const int socketBufferSize = 2 * Packets.Packet.MaxPacketSize;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, socketBufferSize);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, socketBufferSize);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            // TODO: Setting this seems to break things on my Mac!
            // socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

            // Send our version stuffs
            Send($"{ServerProtocolVersion}\r\n");
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

                            SendKexInit();
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
                        var packet = ReadPacket();

                        // TODO - will reading in a loop allow starvation of other clients?
                        while (packet != null)
                        {
                            logger.LogDebug("Received packet: {Type}.", packet.PacketType);

                            // Handle the packet
                            HandlePacket(packet);

                            // Read next packet (if any)
                            packet = ReadPacket();
                        }

                        // TODO - Consider re-exchanging keys
                    }
                    catch (SwishServerException ex)
                    {
                        logger.LogError(ex.Message);
                        Disconnect(ex.Reason, ex.Message);
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Exception reading/processing packet.");
                        Disconnect(DisconnectReason.None, "Server exception.");
                        return;
                    }
                }
            }

            await Task.CompletedTask;
        }


        public void Disconnect(DisconnectReason reason, string message)
        {
            logger.LogDebug("{Id} disconnected - {Reason} - {Message}", Id, reason, message);

            if (IsConnected)
            {
                if (reason != DisconnectReason.None)
                {
                    try
                    {
                        Disconnect disconnect = new Disconnect
                        {
                            Reason = reason,
                            Description = message
                        };

                        Send(disconnect);
                    }
                    catch (Exception ex)
                    {
                        logger.LogInformation(ex, "{Id}: Exception sending disconnect to client.", Id);
                    }
                }

                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    logger.LogInformation(ex, "{Id}: Exception shutting down socket.", Id);
                }

                IsConnected = false;
            }
        }


        private void Send(string message)
        {
            logger.LogDebug("Sending raw string: '{Content}'.", message.Trim());
            Send(Encoding.UTF8.GetBytes(message));
        }


        private void Send(byte[] message)
        {
            if (!IsConnected)
            {
                return;
            }

            totalBytesTransferred += message.Length;

            socket.Send(message);
        }


        private void Send(Packet packet)
        {
            logger.LogDebug("Sending {0} packet.", packet.PacketType);

            packet.PacketSequence = GetSentPacketNumber();

            var payload = packet.GetBytes();

            payload = activeExchangeContext.CompressionServerToClient.Compress(payload);

            uint blockSize = activeExchangeContext.CipherServerToClient.BlockSize;

            byte paddingLength = (byte)((blockSize - (payload.Length + 5)) % blockSize);
            if (paddingLength < 4)
            {
                paddingLength += (byte)blockSize;
            }

            byte[] padding = new byte[paddingLength];

            // Fill padding with random goodness
            RandomNumberGenerator.Create().GetBytes(padding);

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

            // Apply MAC, if we have one
            if (activeExchangeContext.MACAlgorithmServerToClient != null)
            {
                byte[] mac = activeExchangeContext.MACAlgorithmServerToClient.ComputeHash(packet.PacketSequence, payload);
                encryptedPayload = encryptedPayload.Concat(mac).ToArray();
            }

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
            if (activeExchangeContext.MACAlgorithmClientToServer != null)
            {
                byte[] clientMac = new byte[activeExchangeContext.MACAlgorithmClientToServer.DigestLength];
                bytesRead = socket.Receive(clientMac);
                if (bytesRead != activeExchangeContext.MACAlgorithmClientToServer.DigestLength)
                {
                    throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_CONNECTION_LOST, "Failed to read from socket.");
                }

                var mac = activeExchangeContext.MACAlgorithmClientToServer.ComputeHash(packetNumber, fullPacket);
                if (!clientMac.SequenceEqual(mac))
                {
                    throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_MAC_ERROR, "MAC from client is invalid");
                }
            }

            // Decompress
            payload = activeExchangeContext.CompressionClientToServer.Decompress(payload);

            // Parse the packet
            using (var reader = new ByteReader(payload))
            {
                var type = (PacketType)reader.GetByte();

                if (Packet.PacketTypes.ContainsKey(type))
                {
                    Packet packet = Activator.CreateInstance(Packet.PacketTypes[type]) as Packet;
                    packet.Load(reader);
                    packet.PacketSequence = packetNumber;
                    return packet;
                }

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


        private void HandlePacket(Packet packet)
        {
            try
            {
                HandleSpecificPacket((dynamic)packet);
            }
            catch (RuntimeBinderException)
            {
                logger.LogWarning("Unhandled packet type: {Type}.", packet.PacketType);

                Unimplemented unimplemented = new Unimplemented()
                {
                    RejectedPacketNumber = packet.PacketSequence
                };

                Send(unimplemented);
            }
        }


        private void SendKexInit()
        {
            kexInitServerToClient.KexAlgorithms.AddRange(KeyInfo.SupportedKeyExchanges.Select(x => x.Name));
            kexInitServerToClient.EncryptionAlgorithmsClientToServer.AddRange(KeyInfo.SupportedCiphers.Select(x => x.Name));
            kexInitServerToClient.EncryptionAlgorithmsServerToClient.AddRange(KeyInfo.SupportedCiphers.Select(x => x.Name));
            kexInitServerToClient.ServerHostKeyAlgorithms.AddRange(KeyInfo.SupportedHostKeyAlgorithms.Select(x => x.Name));
            kexInitServerToClient.MacAlgorithmsClientToServer.AddRange(KeyInfo.SupportedMACAlgorithms.Select(x => x.Name));
            kexInitServerToClient.MacAlgorithmsServerToClient.AddRange(KeyInfo.SupportedMACAlgorithms.Select(x => x.Name));
            kexInitServerToClient.CompressionAlgorithmsClientToServer.AddRange(KeyInfo.SupportedCompressions.Select(x => x.Name));
            kexInitServerToClient.CompressionAlgorithmsServerToClient.AddRange(KeyInfo.SupportedCompressions.Select(x => x.Name));

            Send(kexInitServerToClient);
        }


        private void HandleSpecificPacket(KexInit packet)
        {
            logger.LogDebug("Processing KexInit packet.");

            // TODO - handle re-exchange

            // Keep track of the client-to-server packet
            kexInitClientToServer = packet;

            // Pick algorithms
            pendingExchangeContext.KexAlgorithm = KeyInfo.PickKexAlgorithm(packet.KexAlgorithms);
            pendingExchangeContext.HostKeyAlgorithm = KeyInfo.PickHostKeyAlgorithm(packet.ServerHostKeyAlgorithms);
            pendingExchangeContext.CipherClientToServer = KeyInfo.PickCipher(packet.EncryptionAlgorithmsClientToServer, "Client-To-Server");
            pendingExchangeContext.CipherServerToClient = KeyInfo.PickCipher(packet.EncryptionAlgorithmsServerToClient, "Server-To-Client");
            pendingExchangeContext.MACAlgorithmClientToServer = KeyInfo.PickMACAlgorithm(packet.MacAlgorithmsClientToServer, "Client-To-Server");
            pendingExchangeContext.MACAlgorithmServerToClient = KeyInfo.PickMACAlgorithm(packet.MacAlgorithmsServerToClient, "Server-To-Client");
            pendingExchangeContext.CompressionClientToServer = KeyInfo.PickCompression(packet.CompressionAlgorithmsClientToServer, "Client-To-Server");
            pendingExchangeContext.CompressionServerToClient = KeyInfo.PickCompression(packet.CompressionAlgorithmsServerToClient, "Server-To-Client");
        }


        private void HandleSpecificPacket(KexDHInit packet)
        {
            logger.LogDebug("Processing KexDHInit packet.");

            if ((pendingExchangeContext == null) || (pendingExchangeContext.KexAlgorithm == null))
            {
                throw new SwishServerException(DisconnectReason.SSH_DISCONNECT_PROTOCOL_ERROR, "Server did not receive SSH_MSG_KEX_INIT as expected.");
            }

            // 1. C generates a random number x (1 &lt x &lt q) and computes e = g ^ x mod p.  C sends e to S.
            // 2. S receives e.  It computes K = e^y mod p
            byte[] sharedSecret = pendingExchangeContext.KexAlgorithm.DecryptKeyExchange(packet.ClientValue);

            // 2. S generates a random number y (0 < y < q) and computes f = g ^ y mod p.
            byte[] serverKeyExchange = pendingExchangeContext.KexAlgorithm.CreateKeyExchange();

            byte[] hostKey = pendingExchangeContext.HostKeyAlgorithm.CreateKeyAndCertificatesData();

            // H = hash(V_C || V_S || I_C || I_S || K_S || e || f || K)
            byte[] exchangeHash = ComputeExchangeHash(
                pendingExchangeContext.KexAlgorithm,
                hostKey,
                packet.ClientValue,
                serverKeyExchange,
                sharedSecret);

            if (sessionId == null)
            {
                sessionId = exchangeHash;
            }

            // https://tools.ietf.org/html/rfc4253#section-7.2

            // Initial IV client to server: HASH(K || H || "A" || session_id)
            // (Here K is encoded as mpint and "A" as byte and session_id as raw
            // data.  "A" means the single character A, ASCII 65).
            byte[] clientCipherIV = ComputeEncryptionKey(
                pendingExchangeContext.KexAlgorithm,
                exchangeHash,
                pendingExchangeContext.CipherClientToServer.BlockSize,
                sharedSecret, 'A');

            // Initial IV server to client: HASH(K || H || "B" || session_id)
            byte[] serverCipherIV = ComputeEncryptionKey(
                pendingExchangeContext.KexAlgorithm,
                exchangeHash,
                pendingExchangeContext.CipherServerToClient.BlockSize,
                sharedSecret, 'B');

            // Encryption key client to server: HASH(K || H || "C" || session_id)
            byte[] clientCipherKey = ComputeEncryptionKey(
                pendingExchangeContext.KexAlgorithm,
                exchangeHash,
                pendingExchangeContext.CipherClientToServer.KeySize,
                sharedSecret, 'C');

            // Encryption key server to client: HASH(K || H || "D" || session_id)
            byte[] serverCipherKey = ComputeEncryptionKey(
                pendingExchangeContext.KexAlgorithm,
                exchangeHash,
                pendingExchangeContext.CipherServerToClient.KeySize,
                sharedSecret, 'D');

            // Integrity key client to server: HASH(K || H || "E" || session_id)
            byte[] clientHmacKey = ComputeEncryptionKey(
                pendingExchangeContext.KexAlgorithm,
                exchangeHash,
                pendingExchangeContext.MACAlgorithmClientToServer.KeySize,
                sharedSecret, 'E');

            // Integrity key server to client: HASH(K || H || "F" || session_id)
            byte[] serverHmacKey = ComputeEncryptionKey(
                pendingExchangeContext.KexAlgorithm,
                exchangeHash,
                pendingExchangeContext.MACAlgorithmServerToClient.KeySize,
                sharedSecret, 'F');

            // Set all keys we just generated
            pendingExchangeContext.CipherClientToServer.SetKey(clientCipherKey, clientCipherIV);
            pendingExchangeContext.CipherServerToClient.SetKey(serverCipherKey, serverCipherIV);
            pendingExchangeContext.MACAlgorithmClientToServer.SetKey(clientHmacKey);
            pendingExchangeContext.MACAlgorithmServerToClient.SetKey(serverHmacKey);

            // Send reply to client!
            var reply = new KexDHReply
            {
                ServerHostKey = hostKey,
                ServerValue = serverKeyExchange,
                Signature = pendingExchangeContext.HostKeyAlgorithm.CreateSignatureData(exchangeHash)
            };

            Send(reply);
            Send(new NewKeys());
        }


        private void HandleSpecificPacket(NewKeys packet)
        {
            logger.LogDebug("Processing NewKeys packet.");

            activeExchangeContext = pendingExchangeContext;
            pendingExchangeContext = null;

            // Reset re-exchange values
            totalBytesTransferred = 0;
            keyTimeout = DateTime.UtcNow.AddHours(1);
        }


        private void HandleSpecificPacket(ServiceRequest packet)
        {
            logger.LogDebug("Processing ServiceRequest packet, service='{Service}'.", packet.ServiceName);

            switch (packet.ServiceName)
            {
                case "ssh-userauth":
                    Send(new ServiceAccept
                    {
                        ServiceName = packet.ServiceName
                    });
                    break;

                default:
                    logger.LogWarning("Service '{Service}' is not yet implemented/supported.", packet.ServiceName);
                    Disconnect(DisconnectReason.SSH_DISCONNECT_SERVICE_NOT_AVAILABLE, "Not a supported service.");
                    break;
            }
        }


        private void HandleSpecificPacket(UserAuthRequest packet)
        {
            logger.LogDebug("Processing UserAuthRequest packet, user='{User}', service='{Service}', method='{Method}'.", packet.UserName, packet.ServiceName, packet.MethodName);

            // TODO - if this is the first user auth request, send a welcome banner

            if (packet.MethodName == "none")
            {
                SendFail();
            }
            else if (packet.MethodName == "password")
            {
                // See https://tools.ietf.org/html/rfc4252#section-8

                // TODO - HACK - simple auth scheme for testing
                if ((packet.UserName == "foo") && (packet.Password == "bar"))
                {
                    Send(new UserAuthSuccess());
                }
                else
                {
                    SendFail();
                }
            }
            else
            {
                SendFail();
            }
        }


        private void SendFail()
        {
            var fail = new UserAuthFailure
            {
                PartialSuccess = false
            };

            fail.AuthTypesThatCanContinue.Add("password");

            Send(fail);
        }


        private byte[] ComputeExchangeHash(IKexAlgorithm kexAlgorithm, byte[] hostKeyAndCerts, byte[] clientExchangeValue, byte[] serverExchangeValue, byte[] sharedSecret)
        {
            // H = hash(V_C || V_S || I_C || I_S || K_S || e || f || K)
            using (ByteWriter writer = new ByteWriter())
            {
                writer.WriteString(protocolVersionExchange);
                writer.WriteString(ServerProtocolVersion);

                writer.WriteBytes(kexInitClientToServer.GetBytes());
                writer.WriteBytes(kexInitServerToClient.GetBytes());
                writer.WriteBytes(hostKeyAndCerts);

                writer.WriteMPInt(clientExchangeValue);
                writer.WriteMPInt(serverExchangeValue);
                writer.WriteMPInt(sharedSecret);

                return kexAlgorithm.ComputeHash(writer.ToByteArray());
            }
        }


        private byte[] ComputeEncryptionKey(IKexAlgorithm kexAlgorithm, byte[] exchangeHash, uint keySize, byte[] sharedSecret, char letter)
        {
            // K(X) = HASH(K || H || X || session_id)

            // Prepare the buffer
            byte[] keyBuffer = new byte[keySize];
            int keyBufferIndex = 0;
            int currentHashLength = 0;
            byte[] currentHash = null;

            // We can stop once we fill the key buffer
            while (keyBufferIndex < keySize)
            {
                using (ByteWriter writer = new ByteWriter())
                {
                    // Write "K"
                    writer.WriteMPInt(sharedSecret);

                    // Write "H"
                    writer.WriteRawBytes(exchangeHash);

                    if (currentHash == null)
                    {
                        // If we haven't done this yet, add the "X" and session_id
                        writer.WriteByte((byte)letter);
                        writer.WriteRawBytes(sessionId);
                    }
                    else
                    {
                        // If the key isn't long enough after the first pass, we need to
                        // write the current hash as described here:
                        //      K1 = HASH(K || H || X || session_id)   (X is e.g., "A")
                        //      K2 = HASH(K || H || K1)
                        //      K3 = HASH(K || H || K1 || K2)
                        //      ...
                        //      key = K1 || K2 || K3 || ...
                        writer.WriteRawBytes(currentHash);
                    }

                    currentHash = kexAlgorithm.ComputeHash(writer.ToByteArray());
                }

                currentHashLength = Math.Min(currentHash.Length, (int)(keySize - keyBufferIndex));
                Array.Copy(currentHash, 0, keyBuffer, keyBufferIndex, currentHashLength);

                keyBufferIndex += currentHashLength;
            }

            return keyBuffer;
        }
    }
}
