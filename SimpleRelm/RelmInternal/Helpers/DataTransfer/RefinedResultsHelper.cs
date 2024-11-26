using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Helpers.Operations;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Helpers.DataTransfer
{
    internal class RefinedResultsHelper
    {
        /// <summary>
        /// Query the database to get a single value
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">SQL query to retrieve the value requested</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
        /// <param name="AllowUserVariables"></param>
        /// <param name="UseTransaction"></param>
        /// <param name="SqlTransaction"></param>
        /// <returns>Single value of type T</returns>
        internal static T GetScalar<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return GetScalar<T>(conn, QueryString, Parameters, ThrowException: ThrowException);
            }
        }

        /// <summary>
        /// Query the database to get a single value
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="EstablishedConnection">An open and established connection to a MySQL database</param>
        /// <param name="QueryString">SQL query to retrieve the value requested</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or swallow and return default(T)</param>
        /// <returns>Single value of type T</returns>
        internal static T GetScalar<T>(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
        {
            return GetScalar<T>(new RelmContext(EstablishedConnection, SqlTransaction), QueryString, Parameters, ThrowException: ThrowException);
        }

        internal static T GetScalar<T>(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
        {
            return DatabaseWorkHelper.DoDatabaseWork<T>(relmContext, QueryString,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(Parameters);

                    var scalarResult = cmd.ExecuteScalar();

                    return CoreUtilities.ConvertScalar<T>(scalarResult);
                },
                ThrowException: ThrowException, UseTransaction: SqlTransaction != null);
        }

        internal static DataRow GetDataRow(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return GetDataRow(conn, QueryString, Parameters, ThrowException: ThrowException);
            }
        }

        internal static DataRow GetDataRow(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
        {
            var intermediate = GetDataTable(EstablishedConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction);

            return intermediate.Rows.Count > 0 ? intermediate.Rows[0] : null;
        }

        /// <summary>
        /// Query the database and return a table object
        /// </summary>
        /// <param name="ConfigConnectionString">A ConnectionStringTypes type to reference a connection string defined in web.config</param>
        /// <param name="QueryString">SQL query to retrieve the table requested</param>
        /// <param name="Parameters">Dictionary of named parameters</param>
        /// <param name="ThrowException">Throw exception or swallow and return null</param>
        /// <returns>DataTable with requested data</returns>
        internal static DataTable GetDataTable(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return GetDataTable(conn, QueryString, Parameters, ThrowException: ThrowException);
            }
        }

        internal static DataTable GetDataTable(MySqlConnection EstablishedConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
        {
            return DatabaseWorkHelper.DoDatabaseWork<DataTable>(EstablishedConnection, QueryString,
                (cmd) =>
                {
                    cmd.Parameters.AddAllParameters(Parameters);

                    using (var tableAdapter = new MySqlDataAdapter())
                    {
                        tableAdapter.SelectCommand = cmd;
                        tableAdapter.SelectCommand.CommandType = CommandType.Text;

                        var outputTable = new DataTable();
                        tableAdapter.Fill(outputTable);

                        return outputTable;
                    }
                }, ThrowException: ThrowException, UseTransaction: SqlTransaction != null, SqlTransaction: SqlTransaction);
        }
    }
}