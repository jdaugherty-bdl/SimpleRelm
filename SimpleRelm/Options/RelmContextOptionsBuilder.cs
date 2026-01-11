using MySql.Data.MySqlClient;
using SimpleRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleRelm.Options
{
    /// <summary>
    /// Provides a builder for configuring options required to establish a connection to a relational database context.
    /// Supports multiple initialization patterns, including connection strings, named connections, and open MySQL
    /// connections.
    /// </summary>
    /// <remarks>Use this class to specify database connection details such as server, database name, user
    /// credentials, or to provide an existing MySqlConnection or named connection. The builder validates configuration
    /// based on the selected connection method. Once configured, the options can be used to initialize a database
    /// context. This class is not thread-safe.</remarks>
    public class RelmContextOptionsBuilder
    {
        /// <summary>
        /// Specifies the types of options that can be used to configure a database context using an options builder.
        /// </summary>
        /// <remarks>Use this enumeration to indicate how the options builder should obtain or interpret
        /// connection information when configuring a database context. The selected value determines whether a raw
        /// connection string, a named connection string, or an existing open connection is used.</remarks>
        public enum OptionsBuilderTypes
        {
            /// <summary>
            /// Sets the option builder connection type to use a raw connection string.
            /// </summary>
            ConnectionString,
            /// <summary>
            /// Sets the option builder connection type to use a named connection string.
            /// </summary>
            NamedConnectionString,
            /// <summary>
            /// Sets the option builder connection type to use an open connection.
            /// </summary>
            OpenConnection
        }

        /// <summary>
        /// Gets the name or network address of the database server to which the application is connected.
        /// </summary>
        public string DatabaseServer { get; private set; }

        /// <summary>
        /// Gets the name of the database associated with this instance.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// Gets the user name used to connect to the database.
        /// </summary>
        public string DatabaseUser { get; private set; }

        /// <summary>
        /// Gets the password used to connect to the database.
        /// </summary>
        public string DatabasePassword { get; private set; }

        /// <summary>
        /// Gets the connection string used to connect to the database.
        /// </summary>
        public string DatabaseConnectionString { get; private set; }

        /// <summary>
        /// Gets or sets the name of the connection to use for database operations.
        /// </summary>
        public string NamedConnection { get; set; }

        /// <summary>
        /// Gets the active MySQL database connection used by the application.
        /// </summary>
        public MySqlConnection DatabaseConnection { get; private set; }

        /// <summary>
        /// Gets the current database transaction associated with the connection.
        /// </summary>
        /// <remarks>Use this property to access the active MySQL transaction for executing commands
        /// within a transactional context. The property is null if no transaction is in progress.</remarks>
        public MySqlTransaction DatabaseTransaction { get; private set; }

        /// <summary>
        /// Gets the type of options builder used to configure options for this instance.
        /// </summary>
        public OptionsBuilderTypes OptionsBuilderType => _optionsBuilderType;
        private OptionsBuilderTypes _optionsBuilderType;

        /// <summary>
        /// Gets the type of the connection string used by the data source.
        /// </summary>
        public Enum ConnectionStringType => _connectionStringType;
        private Enum _connectionStringType;

        internal bool CanOpenConnection { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class.
        /// </summary>
        public RelmContextOptionsBuilder() { }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string used to configure the context options. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if the connectionString parameter is null or empty.</exception>
        public RelmContextOptionsBuilder(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("Connection string cannot be null or empty.", nameof(connectionString));

            ParseConnectionDetails(connectionString);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class with the specified database connection
        /// settings.
        /// </summary>
        /// <param name="databaseServer">The name or network address of the database server to connect to. Cannot be null or empty.</param>
        /// <param name="databaseName">The name of the database to use. Cannot be null or empty.</param>
        /// <param name="databaseUser">The username to use when connecting to the database. Cannot be null or empty.</param>
        /// <param name="databasePassword">The password associated with the specified database user. Cannot be null or empty.</param>
        public RelmContextOptionsBuilder(string databaseServer, string databaseName, string databaseUser, string databasePassword)
        {
            SetDatabaseServer(databaseServer);
            SetDatabaseName(databaseName);
            SetDatabaseUser(databaseUser);
            SetDatabasePassword(databasePassword);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified connection string
        /// type.
        /// </summary>
        /// <param name="connectionStringType">An enumeration value that specifies the type of connection string to use. Must be a valid enum representing
        /// a supported connection string type.</param>
        public RelmContextOptionsBuilder(Enum connectionStringType)
        {
            SetConnectionStringType(connectionStringType.GetType(), connectionStringType);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified MySQL database
        /// connection.
        /// </summary>
        /// <param name="connection">The MySqlConnection to use for configuring the context options. Cannot be null.</param>
        public RelmContextOptionsBuilder(MySqlConnection connection)
        {
            SetDatabaseConnection(connection);
        }

        /// <summary>
        /// Initializes a new instance of the RelmContextOptionsBuilder class using the specified MySqlConnection and
        /// MySqlTransaction.
        /// </summary>
        /// <remarks>Use this constructor to configure the context to operate within an existing MySQL
        /// connection and transaction. This is useful when managing connection and transaction lifetimes
        /// externally.</remarks>
        /// <param name="connection">The MySqlConnection to be used for database operations. Cannot be null.</param>
        /// <param name="transaction">The MySqlTransaction to associate with the context. Cannot be null.</param>
        public RelmContextOptionsBuilder(MySqlConnection connection, MySqlTransaction transaction)
        {
            SetDatabaseConnection(connection);
            SetDatabaseTransaction(transaction);
        }

        /// <summary>
        /// Sets the database connection to be used by the current instance.
        /// </summary>
        /// <remarks>After calling this method, the instance will use the provided connection for all
        /// subsequent database operations. The caller is responsible for managing the lifetime of the
        /// connection.</remarks>
        /// <param name="connection">The open <see cref="MySqlConnection"/> to associate with this instance. The connection must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connection"/> is null.</exception>
        public void SetDatabaseConnection(MySqlConnection connection)
        {
            DatabaseConnection = connection ?? throw new ArgumentNullException("Connection cannot be null.", nameof(connection));

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;
        }

        /// <summary>
        /// Sets the database transaction to be used for subsequent database operations.
        /// </summary>
        /// <remarks>Use this method to specify an existing transaction for database commands. If a
        /// transaction is set, all subsequent operations will be executed within the context of that transaction until
        /// it is cleared or replaced.</remarks>
        /// <param name="transaction">The MySqlTransaction instance to associate with database operations. Can be null to clear the current
        /// transaction.</param>
        public void SetDatabaseTransaction(MySqlTransaction transaction)
        {
            DatabaseTransaction = transaction; // ?? throw new ArgumentNullException("Transaction cannot be null.", nameof(transaction));

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;
        }

        /// <summary>
        /// Sets the database server to use for establishing connections.
        /// </summary>
        /// <param name="databaseServer">The name or address of the database server. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseServer"/> is null or empty.</exception>
        public void SetDatabaseServer(string databaseServer)
        {
            if (string.IsNullOrEmpty(databaseServer))
                throw new ArgumentNullException("Database server cannot be null or empty.", nameof(databaseServer));

            this.DatabaseServer = databaseServer;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        /// <summary>
        /// Sets the name of the database to be used for the connection.
        /// </summary>
        /// <param name="databaseName">The name of the database. Must be a non-empty string containing only alphanumeric characters, underscores
        /// (_), dollar signs ($), or Unicode characters in the range U+0080 to U+FFFF.</param>
        /// <exception cref="ArgumentNullException">Thrown if the databaseName parameter is null or an empty string.</exception>
        /// <exception cref="ArgumentException">Thrown if databaseName contains invalid characters. The name must be alphanumeric and may include
        /// underscores (_), dollar signs ($), or Unicode characters in the range U+0080 to U+FFFF.</exception>
        public void SetDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException("Database name cannot be null or empty.", nameof(databaseName));

            string pattern = @"^[a-zA-Z0-9$_\u0080-\uFFFF]+$";

            if (!Regex.IsMatch(databaseName, pattern))
                throw new ArgumentException("DatabaseName", "Invalid database name. Must be alphanumeric with underscores.");

            this.DatabaseName = databaseName;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        /// <summary>
        /// Sets the database user name to be used for the connection.
        /// </summary>
        /// <param name="databaseUser">The user name to associate with the database connection. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseUser"/> is null or empty.</exception>
        public void SetDatabaseUser(string databaseUser)
        {
            if (string.IsNullOrEmpty(databaseUser))
                throw new ArgumentNullException("Database user cannot be null or empty.", nameof(databaseUser));

            this.DatabaseUser = databaseUser;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        /// <summary>
        /// Sets the password used to connect to the database.
        /// </summary>
        /// <param name="databasePassword">The password to use for authenticating the database connection. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databasePassword"/> is null or empty.</exception>
        public void SetDatabasePassword(string databasePassword)
        {
            if (string.IsNullOrEmpty(databasePassword))
                throw new ArgumentNullException("Database password cannot be null or empty.", nameof(databasePassword));

            this.DatabasePassword = databasePassword;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        /// <summary>
        /// Sets the type of the connection string using the specified enumeration value.
        /// </summary>
        /// <param name="connectionStringType">An enumeration value that specifies the type of connection string to use. Must not be null.</param>
        public void SetConnectionStringType(Enum connectionStringType)
        {
            SetConnectionStringType(connectionStringType.GetType(), connectionStringType);
        }

        /// <summary>
        /// Sets the type of the connection string to use for database operations.
        /// </summary>
        /// <param name="enumType">The enumeration type that defines the valid connection string types. Must be an enum type.</param>
        /// <param name="connectionStringType">The specific connection string type to set. Must be a defined value of <paramref name="enumType"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connectionStringType"/> is not a defined value of <paramref name="enumType"/>.</exception>
        public void SetConnectionStringType(Type enumType, Enum connectionStringType)
        { 
            if (!Enum.IsDefined(enumType, connectionStringType))
                throw new ArgumentNullException("Invalid connection string type provided.", nameof(connectionStringType));

            _connectionStringType = connectionStringType;

            NamedConnection = connectionStringType.ToString();

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

        /// <summary>
        /// Sets the current database connection using the specified named connection string.
        /// </summary>
        /// <param name="namedConnection">The name of the connection string to use. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if the namedConnection parameter is null or empty.</exception>
        public void SetNamedConnection(string namedConnection)
        {
            if (string.IsNullOrEmpty(namedConnection))
                throw new ArgumentNullException(nameof(namedConnection));

            NamedConnection = namedConnection;

            /*
            if (!Enum.TryParse(DatabaseConnectionString, out _connectionStringType))
                throw new ArgumentException($"Invalid connection string type '{DatabaseConnectionString}'.");
            ConnectionStringType = (DALHelper.ConnectionStringTypes)Enum.Parse(typeof(DALHelper.ConnectionStringTypes), DatabaseConnectionString);
            */

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

        /// <summary>
        /// Sets the connection string used to connect to the database.
        /// </summary>
        /// <param name="DatabaseConnectionString">The connection string to use for database connections. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="DatabaseConnectionString"/> is null or empty.</exception>
        public void SetDatabaseConnectionString(string DatabaseConnectionString)
        {
            if (string.IsNullOrEmpty(DatabaseConnectionString))
                throw new ArgumentNullException(nameof(DatabaseConnectionString));

            this.DatabaseConnectionString = DatabaseConnectionString;
        }

        /// <summary>
        /// Validates all required database connection settings based on the configured options builder type.
        /// </summary>
        /// <remarks>The required settings depend on the options builder type. For example, a named
        /// connection string requires DatabaseConnectionString, an open connection requires DatabaseConnection, and a
        /// standard connection string requires DatabaseServer, DatabaseName, DatabaseUser, and
        /// DatabasePassword.</remarks>
        /// <param name="throwExceptions">true to throw an exception if a required setting is missing; false to return false instead of throwing an
        /// exception. The default is true.</param>
        /// <returns>true if all required settings are valid; otherwise, false if a required setting is missing and
        /// throwExceptions is false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if a required setting is missing and throwExceptions is true.</exception>
        /// <exception cref="ArgumentException">Thrown if the configured connection string type is invalid.</exception>
        public bool ValidateAllSettings(bool throwExceptions = true)
        {
            if (_optionsBuilderType == OptionsBuilderTypes.NamedConnectionString)
            {
                if (string.IsNullOrEmpty(DatabaseConnectionString))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseConnectionString", "DatabaseConnectionString cannot be null or empty when using a named connection string.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.OpenConnection)
            {
                if (DatabaseConnection == null)
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseConnection), "Database connection cannot be null.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.ConnectionString)
            {
                if (string.IsNullOrEmpty(DatabaseServer))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseServer", "Database Server cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseName))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseName", "Database Name cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseUser))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseUser", "Username cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabasePassword))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabasePassword", "Password cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                return true;
            }
            else
            {
                throw new ArgumentException($"Invalid connection string type '{ConnectionStringType}'.");
            }
        }

        /// <summary>
        /// Parses the specified connection details string and configures the database connection settings accordingly.
        /// </summary>
        /// <remarks>The method supports two formats for specifying connection details: a named connection
        /// (e.g., 'name=MyConnection') or explicit connection parameters (e.g.,
        /// 'server=localhost;database=MyDb;user=admin;password=secret'). Only one format may be used at a time.
        /// Additional or missing parameters will result in an exception.</remarks>
        /// <param name="connectionDetails">A string containing the connection details. Must be in the format 'name=connectionString' to use a named
        /// connection, or 'server=serverName;database=databaseName;user=userName;password=password' to specify
        /// individual connection parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if the connectionDetails parameter is null, empty, or consists only of white-space characters.</exception>
        /// <exception cref="ArgumentException">Thrown if the connectionDetails parameter does not match the required format or is missing required
        /// connection parameters.</exception>
        public void ParseConnectionDetails(string connectionDetails)
        {
            if (string.IsNullOrWhiteSpace(connectionDetails))
                throw new ArgumentNullException(nameof(connectionDetails));

            if (!connectionDetails.Contains("="))
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");

            var connectionOptions = connectionDetails
                .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(x => x[0].ToLower(), x => x[1]);

            // Check for either a 'name' key or all four individual connection parameters.
            if (!(connectionOptions.ContainsKey("name") ||
                  (connectionOptions.ContainsKey("server") &&
                   connectionOptions.ContainsKey("database") &&
                   (connectionOptions.ContainsKey("uid") || connectionOptions.ContainsKey("user") || connectionOptions.ContainsKey("user id")) &&
                   (connectionOptions.ContainsKey("pwd") || connectionOptions.ContainsKey("password")))))
            {
                throw new ArgumentException("Incomplete connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if ((connectionOptions.ContainsKey("name") &&
                    connectionOptions.Keys.Count > 1) ||
                (connectionOptions.ContainsKey("server") &&
                    connectionOptions.ContainsKey("database") &&
                   (connectionOptions.ContainsKey("uid") || connectionOptions.ContainsKey("user") || connectionOptions.ContainsKey("user id")) &&
                   (connectionOptions.ContainsKey("pwd") || connectionOptions.ContainsKey("password")) &&
                    connectionOptions.Keys.Count > 4))
            {
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if (connectionOptions.ContainsKey("name"))
            {
                SetNamedConnection(connectionOptions["name"]);

                var connectionBuilder = RelmHelper.GetConnectionBuilderFromName(connectionOptions["name"]);
                var connectionString = connectionBuilder?.ConnectionString;

                SetDatabaseConnectionString(connectionString);
            }
            else
            {
                if (connectionOptions.ContainsKey("server"))
                    SetDatabaseServer(connectionOptions["server"]);

                if (connectionOptions.ContainsKey("database"))
                    SetDatabaseName(connectionOptions["database"]);

                if (connectionOptions.ContainsKey("uid"))
                    SetDatabaseUser(connectionOptions["uid"]);
                else if (connectionOptions.ContainsKey("user"))
                    SetDatabaseUser(connectionOptions["user"]);
                else if (connectionOptions.ContainsKey("user id"))
                    SetDatabaseUser(connectionOptions["user id"]);

                if (connectionOptions.ContainsKey("pwd"))
                    SetDatabasePassword(connectionOptions["pwd"]);
                else if (connectionOptions.ContainsKey("password"))
                    SetDatabasePassword(connectionOptions["password"]);

                DatabaseConnectionString = $"server={DatabaseServer};database={DatabaseName};user id={DatabaseUser};password={DatabasePassword}";
            }
        }
    }
}
