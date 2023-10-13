using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces.Resolvers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver_MySQL : IRelmResolver_MySQL
    {
        // if no other DAL Resolvers are specified in the client program, this one is used
        public MySqlConnectionStringBuilder GetConnectionBuilder(Enum ConnectionType)
        {
            // converts the enum name directly to string and then looks for that in the configuration file
            return GetConnectionBuilder(ConnectionType.ToString());
        }

        public MySqlConnectionStringBuilder GetConnectionBuilder(string ConfigConnectionString)
        {
            return new MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[ConfigConnectionString].ConnectionString);
        }

        DbConnectionStringBuilder IRelmResolverBase.GetConnectionBuilder(Enum ConnectionType)
        {
            return GetConnectionBuilder(ConnectionType);
        }

        DbConnectionStringBuilder IRelmResolverBase.GetConnectionBuilder(string ConnectionString)
        {
            return GetConnectionBuilder(ConnectionString);
        }
    }
}
