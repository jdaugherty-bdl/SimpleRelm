using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Interfaces.Resolvers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver : IRelmResolver_MySQL
    {
        // if no other DAL Resolvers are specified in the client program, this one is used
        public MySqlConnectionStringBuilder GetConnectionBuilder(Enum ConnectionType)
        {
            // converts the enum name directly to string and then looks for that in the configuration file
            return GetConnectionBuilder(ConfigurationManager.ConnectionStrings[ConnectionType.ToString()].ConnectionString);
        }

        public MySqlConnectionStringBuilder GetConnectionBuilder(string ConfigConnectionString)
        {
            return new MySqlConnectionStringBuilder(ConfigConnectionString);
        }
    }
}
