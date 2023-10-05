using MySql.Data.MySqlClient;
using SimpleRelm.RelmInternal.Resolvers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.RelmInternal.Resolvers
{
    public class DefaultRelmResolver_Tester
    {
        enum ConnectionType
        {
            SimpleRelmMySql
        }

        [Fact]
        public void GetConnectionBuilderFromConnectionType_InvalidConnection_ShouldThrowException()
        {
            // Arrange, Act & Assert
            Assert.Throws<NullReferenceException>(() => new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["INVALID"].ConnectionString));
        }

        [Fact]
        public void GetConnectionBuilderFromConnectionType_MySql()
        {
            // Arrange
            var resolver = new DefaultRelmResolver();
            var expected = new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["SimpleRelmMySql"].ConnectionString);

            // Act
            var actual = resolver.GetConnectionBuilderFromConnectionType(ConnectionType.SimpleRelmMySql);

            // Assert
            Assert.Equal(expected.ConnectionString, actual.ConnectionString);
        }
    }
}
