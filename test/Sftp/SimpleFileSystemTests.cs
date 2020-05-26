
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Swish.Sftp.Subsystems.Sftp;
using Xunit;


namespace Swish.Sftp.Tests.Sftp
{
    public class SimpleFileSystemTests
    {
        // private readonly Mock<IConfiguration> config = new Mock<IConfiguration>();
        private readonly Mock<ILogger> logger = new Mock<ILogger>();

        private readonly Mock<IConfigurationSection> section = new Mock<IConfigurationSection>();

        private readonly SimpleFileSystem fs;


        public SimpleFileSystemTests()
        {
            var settings = new Dictionary<string, string>
            {
                { "vfs:rootPath", "../../../Sftp/TestRootDir" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            fs = new SimpleFileSystem(configuration, logger.Object);
        }


        [Theory]
        [InlineData("subdir1", true)]
        [InlineData("does-not-exist", false)]
        public void CanReadDirectoryIfItExists(string path, bool expected)
        {
            // Act
            var actual = fs.CanReadDirectory(path);

            // Assert
            actual.Should().Be(expected);
        }


        [Fact]
        public void RootContainsExpectedFiles()
        {
            // Act
            var files = fs.GetFilesInDirectory("/").ToList();

            // Assert
            files.Should().HaveCountGreaterThan(0);

            files.Should().Contain(x => x.Name == "subdir1");
            files.Should().Contain(x => x.Name == "file1.txt");
        }
    }
}
