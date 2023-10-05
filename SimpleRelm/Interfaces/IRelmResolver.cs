using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces
{
    public interface IRelmResolver
    {
        MySqlConnectionStringBuilder GetConnectionBuilder(Enum ConnectionType);
        MySqlConnectionStringBuilder GetConnectionBuilder(string ConnectionString);
    }
}
