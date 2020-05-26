
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Swish.Sftp.Subsystems.Sftp;
using Swish.Sftp.Subsystems.Sftp.Packets;
using Xunit;


namespace Swish.Sftp.Tests.Sftp
{
    public class SftpHandleTests
    {
        private const string HandleName = "bruno";
        private const string DirectoryName = "/foo/bar";
        private const uint RequestId = 42;

        private readonly Mock<ISftpSubsystem> sftp = new Mock<ISftpSubsystem>();
        private readonly Mock<IVirtualFileSystem> vfs = new Mock<IVirtualFileSystem>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();

        private readonly SftpHandle handle;

        private SftpPacket sentPacket;


        public SftpHandleTests()
        {
            handle = new SftpHandle(sftp.Object, HandleName, vfs.Object, logger.Object);

            sftp.Setup(x => x.Send(It.IsAny<SftpPacket>()))
                .Callback<SftpPacket>(x => sentPacket = x);
        }


        [Fact]
        public void OpenDirFetchesFileInfo()
        {
            // Arrange
            vfs.Setup(x => x.GetFilesInDirectory(DirectoryName))
                .Returns(BuildFiles(1))
                .Verifiable();

            // Act
            handle.OpenDir(DirectoryName);

            // Assert
            vfs.Verify();

            sentPacket.Should().BeNull();
        }


        [Fact]
        public void EmptyDirectoryReturnsNoFiles()
        {
            // Arrange
            SetupDir(0);

            // Act
            handle.ReadDir(RequestId);

            // Assert
            sentPacket.Should().BeOfType<StatusPacket>();

            var status = sentPacket as StatusPacket;

            status.Id.Should().Be(RequestId);
            status.StatusCode.Should().Be(StatusPacket.Eof);
        }


        [Fact]
        public void DirectoryWithOneFileReturnsIt()
        {
            // Arrange
            SetupDir(1);

            // Act
            handle.ReadDir(RequestId);

            // Assert
            sentPacket.Should().BeOfType<NamePacket>();

            // TODO - verify the packet contains the file
        }


        private void SetupDir(int count)
        {
            vfs.Setup(x => x.GetFilesInDirectory(DirectoryName))
                .Returns(BuildFiles(count))
                .Verifiable();

            handle.OpenDir(DirectoryName);
        }


        private List<VirtualFileItem> BuildFiles(int count)
        {
            List<VirtualFileItem> files = new List<VirtualFileItem>();

            foreach (var item in Enumerable.Range(1, count))
            {
                files.Add(new VirtualFileItem
                {
                    Name = $"file-{item}.zip"
                });
            }

            return files;
        }
    }
}
