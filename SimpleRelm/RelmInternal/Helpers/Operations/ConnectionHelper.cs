using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Interfaces.Resolvers;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using SimpleRelm.RelmInternal.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace SimpleRelm.RelmInternal.Helpers.Operations
{
    internal class ConnectionHelper
    {
        // a pointer to the application's resolver instance
        internal static IRelmResolver_MySQL DALResolver = GetResolverInstance();

        /// <summary>
        /// find an object inheriting from IDALResolver, but only look in the entry assembly (where all your custom code is)
        /// once it is found, then that object is loaded through Reflection to be used later on.
        /// </summary>
        /// <returns>The application's DALResolver instance.</returns>
        internal static IRelmResolver_MySQL GetResolverInstance()
        {
            // try to get the resolver the standard way
            var entryAssembly = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => !string.IsNullOrWhiteSpace(x.EntryPoint?.Name))
                .SelectMany(x => x
                    .GetModules()
                    .SelectMany(y => y
                        .GetTypes()
                        .Where(z => z
                            .GetInterfaces()
                            .Any(a => a == typeof(IRelmResolver_MySQL)))))
                .FirstOrDefault();

            /*
            var ddd = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x
                    .GetCustomAttributes(true)
                    .Any(y => y is AssemblyCompanyAttribute attribute
                        && !x.FullName.StartsWith("log4net", StringComparison.InvariantCultureIgnoreCase)
                        && !x.FullName.StartsWith("Newtonsoft.Json", StringComparison.InvariantCultureIgnoreCase)
                        && !attribute.Company.StartsWith("Oracle", StringComparison.InvariantCultureIgnoreCase)
                        && !attribute.Company.StartsWith("Microsoft", StringComparison.InvariantCultureIgnoreCase)
                        && !attribute.Company.StartsWith("Umbraco", StringComparison.InvariantCultureIgnoreCase)))
                .ToList();
            var ddd = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x
                    .GetCustomAttributes(true)
                    .Any(y => y is AssemblyCompanyAttribute attribute
                        && (attribute.Company.StartsWith("BV", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau Veritas", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau-Veritas", StringComparison.InvariantCultureIgnoreCase))))
                .ToList();

            var e1 = ddd.Select(x => x.GetModules()).ToList();
            var e11 = e1[0][0].GetTypes();
            var e12 = e1[1][0].GetTypes();
            var e13 = e1[2][0].GetTypes();
            var e11 = Assembly.LoadFrom(ddd[0].Location).GetModules();
            var e12 = Assembly.LoadFrom(ddd[1].Location).GetModules();
            var e13 = Assembly.LoadFrom(ddd[2].Location).GetModules();
            var e2 = ddd.Select(x => Assembly.LoadFrom(x.Location).GetModules().SelectMany(y => y.GetTypes()).ToList()).ToList();
            var eee = ddd.Select(x => x.GetModules().Select(y => y?.GetTypes()?.Select(z => z?.GetInterfaces())?.ToList())?.ToList())?.ToList();

            // if the standard way didn't work, do a little detective work (may not work 100% of the time)
            var clientDalResolverType =
                entryAssembly
                ??
                AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x
                    .GetCustomAttributes(true)
                    .Any(y => y is AssemblyCompanyAttribute attribute
                        && !attribute.Company.StartsWith("Microsoft", StringComparison.InvariantCultureIgnoreCase)
                        && !x.FullName.StartsWith("log4net", StringComparison.InvariantCultureIgnoreCase)
                        && !attribute.Company.StartsWith("Umbraco", StringComparison.InvariantCultureIgnoreCase)))
                .SelectMany(x => x
                    .GetModules()
                    .SelectMany(y => y?
                        .GetTypes()?
                        .Where(z => z?
                            .GetInterfaces()?
                            .Any(a => a == typeof(IRelmResolver_MySQL))
                            ??
                            false)))
                .FirstOrDefault();
            */

            // if the standard way didn't work, do a little detective work (may not work 100% of the time)
            var clientDalResolverType =
                entryAssembly
                ??
                AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x
                    .GetCustomAttributes(true)
                    .Any(y => y is AssemblyCompanyAttribute attribute
                        && (attribute.Company.StartsWith("BV", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau Veritas", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau-Veritas", StringComparison.InvariantCultureIgnoreCase))))
                .SelectMany(x => x
                    .GetModules()
                    .SelectMany(y => y?
                        .GetTypes()?
                        .Where(z => z?
                            .GetInterfaces()?
                            .Any(a => a == typeof(IRelmResolver_MySQL))
                            ??
                            false)))
                .FirstOrDefault();

            // if a resolver is found use that, otherwise use the simple default resolver
            if (clientDalResolverType != null)
                return (IRelmResolver_MySQL)Activator.CreateInstance(clientDalResolverType);
            else
                return new DefaultRelmResolver();
        }

        /// <summary>
        /// Gets a MySQL connection builder that is then used to establish a connection to the database
        /// </summary>
        /// <param name="connectionType">A properly formatted database connection string</param>
        /// <returns>A connection string builder that can be used to establish connections</returns>
        internal static MySqlConnectionStringBuilder GetConnectionBuilderFromType(Enum connectionType)
        {
            return DALResolver?.GetConnectionBuilderFromType(connectionType);
        }

        internal static MySqlConnectionStringBuilder GetConnectionBuilderFromName(string connectionName)
        {
            return DALResolver?.GetConnectionBuilderFromName(connectionName);
        }

        internal static MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string connectionString)
        {
            return DALResolver?.GetConnectionBuilderFromConnectionString(connectionString);
        }

        internal static MySqlConnection GetConnectionFromName(string connectionName, bool allowUserVariables = false)
        {
            var connectionBuilder = GetConnectionBuilderFromName(connectionName);

            return GetConnection(connectionBuilder, allowUserVariables);
        }

        internal static MySqlConnection GetConnectionFromType(Enum connectionType, bool allowUserVariables = false)
        {
            var connectionBuilder = GetConnectionBuilderFromType(connectionType);

            return GetConnection(connectionBuilder, allowUserVariables);
        }

        internal static MySqlConnection GetConnectionFromConnectionString(string connectionString, bool allowUserVariables = false)
        {
            var connectionBuilder = GetConnectionBuilderFromConnectionString(connectionString);

            return GetConnection(connectionBuilder, allowUserVariables);
        }

        private static MySqlConnection GetConnection(MySqlConnectionStringBuilder connectionBuilder, bool allowUserVariables = false)
        { 
            connectionBuilder.ConvertZeroDateTime = true;

            if (allowUserVariables)
                connectionBuilder.AllowUserVariables = true;

            return new MySqlConnection(connectionBuilder.ToString());
        }
    }
}
