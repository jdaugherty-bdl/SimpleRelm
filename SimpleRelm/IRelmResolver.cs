using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm
{
    public interface IRelmResolver
    {
        MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionType(Enum ConfigConnectionString);
        MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string ConfigConnectionString);
    }
}
