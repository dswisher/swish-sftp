
using System.Collections.Generic;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Swish.Sftp.Packets;
using Xunit;

namespace Swish.Sftp.Tests
{
    public class ChannelTests
    {
        private const int ServerChannelId = 13;
        private const int ClientChannelId = 7;
        private const int WindowSize = 342516;
        private const int MaximumPacketSize = 32768;

        private const string Name = "a-variable";
        private const string Value = "some-value";

        private readonly Mock<IPacketSender> sender = new Mock<IPacketSender>();
        private readonly Mock<IConfiguration> config = new Mock<IConfiguration>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();

        private readonly List<Packet> packets = new List<Packet>();

        private readonly Channel channel;


        public ChannelTests()
        {
            var packet = new ChannelOpen
            {
                InitialWindowSize = WindowSize,
                MaximumPacketSize = MaximumPacketSize,
                SenderChannel = ClientChannelId
            };

            channel = new Channel(sender.Object, config.Object, logger.Object, ServerChannelId, packet);

            sender.Setup(x => x.Send(It.IsAny<Packet>()))
                .Callback<Packet>(p => packets.Add(p));
        }


        [Fact]
        public void InitSendsSuccessPacket()
        {
            // Act
            channel.Init();

            // Assert
            packets.Should().HaveCount(1);

            var confirmation = packets[0] as ChannelOpenConfirmation;

            confirmation.RecipientChannel.Should().Be(ClientChannelId);
            confirmation.SenderChannel.Should().Be(ServerChannelId);
            confirmation.InitialWindowSize.Should().Be(WindowSize);
            confirmation.MaximumPacketSize.Should().Be(MaximumPacketSize);
        }


        [Fact]
        public void EnvironmentVariablesAreSaved()
        {
            // Arrange
            var request = new ChannelRequest
            {
                RequestType = "env",
                VariableName = Name,
                VariableValue = Value
            };

            // Act
            channel.HandlePacket(request);

            // Assert
            packets.Should().HaveCount(0);      // TODO - test setting env var with WantReply = true

            channel.GetEnvironmentVariable(Name).Should().Be(Value);
            channel.GetEnvironmentVariable("foo").Should().BeNull();
        }


        [Theory]
        [InlineData("exec")]
        [InlineData("shell")]
        public void UnhandledRequestTypesAreRejected(string requestType)
        {
            // Arrange
            var request = new ChannelRequest
            {
                RequestType = requestType,
                WantReply = true
            };

            // Act
            channel.HandlePacket(request);

            // Assert
            packets.Should().HaveCount(1);

            packets[0].Should().BeOfType<ChannelFailure>();
        }


        [Theory]
        [InlineData("sftp", true)]
        [InlineData("boom", false)]
        public void CanCreateKnownSubsystem(string name, bool known)
        {
            // Arrange
            var request = new ChannelRequest
            {
                RequestType = "subsystem",
                SubsystemName = name,
                WantReply = true        // TODO - add test for WantReply = false; RFC only recommends true
            };

            // Act
            channel.HandlePacket(request);

            // Assert
            packets.Should().HaveCount(1);

            if (known)
            {
                channel.SubsystemName.Should().Be(name);

                packets[0].Should().BeOfType<ChannelSuccess>();
            }
            else
            {
                channel.SubsystemName.Should().BeNull();

                packets[0].Should().BeOfType<ChannelFailure>();
            }
        }
    }
}
