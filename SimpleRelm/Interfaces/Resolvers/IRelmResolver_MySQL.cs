using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces.Resolvers
{
    public interface IRelmResolver_MySQL : IRelmResolverBase
    {
        new MySqlConnectionStringBuilder GetConnectionBuilderFromType(Enum ConnectionType);
        new MySqlConnectionStringBuilder GetConnectionBuilderFromName(string ConnectionString);
        MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string connectionString);
    }
}
