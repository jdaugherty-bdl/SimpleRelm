using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Options
{
    public class RelmContextOptionsBuilder
    {
        public enum OptionsBuilderTypes
        {
            ConnectionString,
            NamedConnectionString
        }

        public string DatabaseServer { get; private set; }
        public string DatabaseName { get; private set; }
        public string DatabaseUser { get; private set; }
        public string DatabasePassword { get; private set; }
        public string DatabaseConnectionString { get; private set; }

        private OptionsBuilderTypes _optionsBuilderType;
        public OptionsBuilderTypes OptionsBuilderType => _optionsBuilderType;

        private Enum _connectionStringType;
        public Enum ConnectionStringType => _connectionStringType;

        public RelmContextOptionsBuilder() { }

        public RelmContextOptionsBuilder(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("Connection string cannot be null or empty.", nameof(connectionString));

            ParseConnectionDetails(connectionString);
        }

        public RelmContextOptionsBuilder(string databaseServer, string databaseName, string databaseUser, string databasePassword)
        {
            if (string.IsNullOrEmpty(databaseServer))
                throw new ArgumentNullException("Database server cannot be null or empty.", nameof(databaseServer));

            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException("Database name cannot be null or empty.", nameof(databaseName));

            if (string.IsNullOrEmpty(databaseUser))
                throw new ArgumentNullException("Database user cannot be null or empty.", nameof(databaseUser));

            if (string.IsNullOrEmpty(databasePassword))
                throw new ArgumentNullException("Database password cannot be null or empty.", nameof(databasePassword));

            DatabaseServer = databaseServer;
            DatabaseName = databaseName;
            DatabaseUser = databaseUser;
            DatabasePassword = databasePassword;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public RelmContextOptionsBuilder(Enum connectionStringType)
        {
            if (!Enum.IsDefined(typeof(Enum), connectionStringType))
                throw new ArgumentNullException("Invalid connection string type provided.", nameof(connectionStringType));

            _connectionStringType = connectionStringType;

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

        // create set methods for each property
        public void SetDatabaseServer(string DatabaseServer)
        {
            this.DatabaseServer = DatabaseServer;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabaseName(string DatabaseName)
        {
            this.DatabaseName = DatabaseName;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabaseUser(string DatabaseUser)
        {
            this.DatabaseUser = DatabaseUser;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabasePassword(string DatabasePassword)
        {
            this.DatabasePassword = DatabasePassword;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabaseConnectionString(string DatabaseConnectionString)
        {
            /*
            if (!Enum.TryParse(DatabaseConnectionString, out _connectionStringType))
                throw new ArgumentException($"Invalid connection string type '{DatabaseConnectionString}'.");
            */

            this.DatabaseConnectionString = DatabaseConnectionString;

            //ConnectionStringType = (DALHelper.ConnectionStringTypes)Enum.Parse(typeof(DALHelper.ConnectionStringTypes), DatabaseConnectionString);

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

        public void SetConnectionStringType(Enum ConnectionStringType)
        {
            _connectionStringType = ConnectionStringType;

            DatabaseConnectionString = ConnectionStringType.ToString();

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

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

        public void ParseConnectionDetails(string connectionDetails)
        {
            if (string.IsNullOrWhiteSpace(connectionDetails))
                throw new ArgumentNullException(nameof(connectionDetails));

            var connectionOptions = connectionDetails
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(x => x[0].ToLower(), x => x[1]);

            // Check for either a 'name' key or all four individual connection parameters.
            if (!(connectionOptions.ContainsKey("name") ||
                  (connectionOptions.ContainsKey("server") &&
                   connectionOptions.ContainsKey("database") &&
                   connectionOptions.ContainsKey("user") &&
                   connectionOptions.ContainsKey("password"))))
            {
                throw new ArgumentException("Incomplete connection details. Either provide a 'name' or 'server', 'database', 'user', and 'password'.");
            }

            if (connectionOptions.ContainsKey("name"))
                SetDatabaseConnectionString(connectionOptions["name"]);
            else
            {
                if (connectionOptions.ContainsKey("server"))
                    SetDatabaseServer(connectionOptions["server"]);

                if (connectionOptions.ContainsKey("database"))
                    SetDatabaseName(connectionOptions["database"]);

                if (connectionOptions.ContainsKey("user"))
                    SetDatabaseUser(connectionOptions["user"]);

                if (connectionOptions.ContainsKey("password"))
                    SetDatabasePassword(connectionOptions["password"]);
            }
        }
    }
}
