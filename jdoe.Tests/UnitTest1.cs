
using Xunit;
using jdoe;
using System.Collections.Generic;

namespace jdoe.Tests;

public class UnitTest1
{
    [Fact]
        public void TestCreate()
        {
		// Arrange
		var options = new Verbs.CreateOptions
		{
			Verbose = 1
            };

            // Act
            int result = Program.ParseOptions(new string[] { "create"}).Result;

            // Assert
            Assert.Equal(0, result);
           
        }
}