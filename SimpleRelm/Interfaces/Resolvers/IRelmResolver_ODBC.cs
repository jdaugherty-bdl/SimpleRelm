using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces.Resolvers
{
    public interface IRelmResolver_ODBC
    {
        OdbcConnectionStringBuilder GetConnectionBuilder(Enum ConnectionType);
        OdbcConnectionStringBuilder GetConnectionBuilder(string ConnectionString);
    }
}
