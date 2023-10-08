using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.Options.RelmContextOptionBuilder_Tests
{
    public class SetDatabaseConnectionStringTests
    {
        [Fact]
        public void SetDatabaseConnectionString_SetsDatabaseConnectionStringPropertyCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedDatabaseConnectionString = "PortalCertDatabase";

            // Act
            builder.SetDatabaseConnectionString(expectedDatabaseConnectionString);

            // Assert
            Assert.Equal(expectedDatabaseConnectionString, builder.DatabaseConnectionString);
        }

        [Fact]
        public void SetDatabaseConnectionString_SetsConnectionStringTypeCorrectly()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedConnectionStringType = RelmContextOptionsBuilder.OptionsBuilderTypes.NamedConnectionString; // Make sure this is valid

            // Act
            builder.SetDatabaseConnectionString("PortalCertDatabase");

            // Assert
            Assert.Equal(expectedConnectionStringType, builder.OptionsBuilderType);
        }

        [Fact]
        public void SetDatabaseConnectionString_ThrowsExceptionForInvalidConnectionStringType()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var invalidConnectionStringType = "InvalidType";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.SetDatabaseConnectionString(invalidConnectionStringType));
        }

        [Fact]
        public void SetDatabaseConnectionString_SetsOptionsBuilderTypeToNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            var expectedType = RelmContextOptionsBuilder.OptionsBuilderTypes.NamedConnectionString;
            var validConnectionStringType = "PortalCertDatabase"; // Make sure this is valid

            // Act
            builder.SetDatabaseConnectionString(validConnectionStringType);

            // Assert
            Assert.Equal(expectedType, builder.OptionsBuilderType);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionString()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseConnectionString("PortalCertDatabase"); // Make sure this is valid

            // Act
            var result = builder.ValidateAllSettings();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateAllSettings_ReturnsTrue_ForValidNamedConnectionString_WithFalseParameter()
        {
            // Arrange
            var builder = new RelmContextOptionsBuilder();
            builder.SetDatabaseConnectionString("PortalCertDatabase"); // Make sure this is valid

            // Act
            var result = builder.ValidateAllSettings(false);

            // Assert
            Assert.True(result);
        }
    }
}
