
using Xunit;
using jdoe;
using System.Collections.Generic;

namespace jdoe.Tests;

public class UnitTest1
{
    [Fact]
        public void TestVerboseOutput()
        {
            // Arrange
            var options = new Options
            {
                Verbose = 1,
                InputFiles = new List<string> { "file1", "file2" },
                InputAliases = new List<string> { "alias1", "alias2" }
            };

            // Act
            var result = Program.ParseOptions(new string[] { "-v", "1", "-r", "file1", "file2", "-a", "alias1", "alias2" });

            // Assert
            Assert.Equal(options.Verbose, result.Verbose);
            Assert.Equal(options.InputFiles, result.InputFiles!);
            Assert.Equal(options.InputAliases, result.InputAliases!);
        }
}