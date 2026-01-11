using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces.Resolvers
{
    /// <summary>
    /// Defines methods for obtaining ODBC connection string builders based on connection type or connection string.
    /// </summary>
    /// <remarks>This interface extends IRelmResolverBase to provide ODBC-specific connection string
    /// resolution. Implementations are expected to return configured OdbcConnectionStringBuilder instances for use in
    /// establishing ODBC database connections.</remarks>
    public interface IRelmResolver_ODBC : IRelmResolverBase
    {
        /// <summary>
        /// Creates and returns a new <see cref="OdbcConnectionStringBuilder"/> configured for the specified connection type.
        /// </summary>
        /// <param name="ConnectionType">An enumeration value that specifies the type of connection for which to build the connection string. The
        /// value must correspond to a supported connection type.</param>
        /// <returns>A new instance of <see cref="OdbcConnectionStringBuilder"/> configured according to the specified connection type.</returns>
        new OdbcConnectionStringBuilder GetConnectionBuilder(Enum ConnectionType);

        /// <summary>
        /// Creates a new instance of the <see cref="OdbcConnectionStringBuilder"/> class initialized with the specified
        /// connection string.
        /// </summary>
        /// <param name="ConnectionString">The connection string to use for initializing the <see cref="OdbcConnectionStringBuilder"/>. Cannot be null.</param>
        /// <returns>A new <see cref="OdbcConnectionStringBuilder"/> initialized with the specified connection string.</returns>
        new OdbcConnectionStringBuilder GetConnectionBuilder(string ConnectionString);
    }
}
