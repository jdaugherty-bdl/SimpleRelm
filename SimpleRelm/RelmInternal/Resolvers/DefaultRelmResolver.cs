using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver : IRelmResolver
    {
        // if no other DAL Resolvers are specified in the client program, this one is used
        public MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString)
        {
            // converts the enum name directly to string and then looks for that in the configuration file
            return GetConnectionBuilderFromConnectionString(ConfigurationManager.ConnectionStrings[ConfigConnectionString.ToString()].ConnectionString);
        }

        public MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string ConfigConnectionString)
        {
            return new MySqlConnectionStringBuilder(ConfigConnectionString);
        }
    }
}
