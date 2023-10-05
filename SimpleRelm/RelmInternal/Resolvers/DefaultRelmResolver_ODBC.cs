using SimpleRelm.Interfaces.Resolvers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Resolvers
{
    internal class DefaultRelmResolver_ODBC : IRelmResolver_ODBC
    {
        public OdbcConnectionStringBuilder GetConnectionBuilder(Enum ConfigConnectionString)
        {
            return GetConnectionBuilder(ConfigurationManager.ConnectionStrings[ConfigConnectionString.ToString()].ConnectionString);
        }

        public OdbcConnectionStringBuilder GetConnectionBuilder(string ConnectionString)
        {
            return new OdbcConnectionStringBuilder(ConnectionString);
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
