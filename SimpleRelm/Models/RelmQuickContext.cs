using MySql.Data.MySqlClient;
using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Models
{
    public class RelmQuickContext : IDisposable, IRelmQuickContext
    {
        public RelmContextOptionsBuilder ContextOptions { get; private set; }

        private IEnumerable<PropertyInfo> _attachedProperties;

        private bool localOpenConnection = false;
        private bool localOpenTransaction = false;

        public RelmQuickContext(RelmContextOptionsBuilder optionsBuilder, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false)
        {
            ContextOptions = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder), "RelmContextOptionsBuilder cannot be null.");

            ContextOptions.ValidateAllSettings();

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables);
        }

        public RelmQuickContext(Enum connectionStringType, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionStringType);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables);
        }

        public RelmQuickContext(string connectionDetails, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionDetails);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables);
        }

        public RelmQuickContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables);
        }

        public RelmQuickContext(MySqlConnection connection, MySqlTransaction transaction, bool autoOpenConnection = true, bool allowUserVariables = false)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection, transaction);

            InitializeContext(autoOpenConnection: autoOpenConnection, autoOpenTransaction: false, allowUserVariables: allowUserVariables);
        }

        private void InitializeContext(bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false)
        {
            if (ContextOptions.DatabaseConnection == null)
                ContextOptions.SetDatabaseConnection(RelmHelper.GetConnectionFromConnectionString(ContextOptions.DatabaseConnectionString, allowUserVariables: allowUserVariables));

            if ((autoOpenConnection || autoOpenTransaction) && ContextOptions.DatabaseConnection != null)
                StartConnection(autoOpenTransaction);
        }

        ~RelmQuickContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            // Implement full disposable pattern
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            EndConnection();

            if (disposing)
            {
                foreach (var attachedProperty in _attachedProperties)
                {
                    if (attachedProperty.GetValue(this) is IDisposable disposable)
                        disposable.Dispose();
                    else
                        attachedProperty.SetValue(this, default);
                }
            }
        }

        public void StartConnection(bool autoOpenTransaction = false)
        {
            if (ContextOptions.DatabaseConnection == null)
                throw new InvalidOperationException("Cannot open a non-existent database connection.");

            if (ContextOptions.DatabaseConnection.State == System.Data.ConnectionState.Closed)
            {
                ContextOptions.DatabaseConnection.Open();

                localOpenConnection = true;
            }

            if (autoOpenTransaction && ContextOptions.DatabaseConnection.State == System.Data.ConnectionState.Open)
            {
                ContextOptions.SetDatabaseTransaction(ContextOptions.DatabaseConnection.BeginTransaction());

                localOpenTransaction = true;
            }
        }

        public void EndConnection(bool commitTransaction = true)
        {
            if ((ContextOptions?.DatabaseConnection?.State ?? System.Data.ConnectionState.Closed) != System.Data.ConnectionState.Closed)
            {
                if (commitTransaction && localOpenTransaction)
                {
                    ContextOptions.DatabaseTransaction?.Commit();

                    localOpenTransaction = false;
                }

                if (localOpenConnection)
                {
                    ContextOptions.DatabaseConnection.Close();

                    localOpenConnection = false;
                }
            }
        }

        public IRelmDataSet<T> GetDataSet<T>() where T : RelmModel, new()
        {
            return GetDataSet<T>(false); // auto-initialize
        }

        public IRelmDataSet<T> GetDataSet<T>(bool throwException) where T : RelmModel, new()
        {
            if ((_attachedProperties?.Count() ?? 0) == 0)
            {
                // cache attached properties to avoid reflection overhead on each call

                // find any properties that are IRelmDataSet<>
                _attachedProperties = this.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>));

                var tableNames = _attachedProperties
                    .Select(prop => (prop.PropertyType.GetGenericArguments()[0].GetCustomAttribute<RelmTable>(false)?.TableName, prop))
                    .Where(x => !string.IsNullOrWhiteSpace(x.TableName))
                    .ToList();

                var currentDatabaseTables = RelmHelper.GetDataList<string>(this, "SHOW TABLES;")
                    .ToList();

                // don't initialize the data sets if the table name is not in the current database
                _attachedProperties = tableNames
                    .Where(x => currentDatabaseTables.Contains(x.TableName))
                    .Select(x => x.prop)
                    .ToList();
            }

            var attachedProperty = _attachedProperties.FirstOrDefault(x => x.PropertyType.GetGenericArguments().Any(y => y == typeof(T)))
                ?? _attachedProperties.FirstOrDefault(x => x.PropertyType.GetGenericArguments().Any(y => y.IsAssignableFrom(typeof(T))))
                ?? throw new InvalidOperationException($"No attached property found for type {typeof(T).Name}.");

            //var dataSet = _attachedDataSets.FirstOrDefault(ds => ds.GetType().GetGenericArguments()[0] == typeof(T)) as IRelmDataSet<T>;
            var dataSet = attachedProperty.GetValue(this) as IRelmDataSet<T>;
            if (dataSet == null && throwException)
                throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");
            else if (dataSet == null)
            {
                // create a default data loader for the generic type argument then create a dataset and pass the data loader
                object dalDataLoader = null;
                var classDataLoader = typeof(T).GetCustomAttribute<RelmDataLoader>(true);
                if (classDataLoader == null)
                    dalDataLoader = Activator.CreateInstance(typeof(RelmDefaultDataLoader<>).MakeGenericType(typeof(T)), new object[] { ContextOptions });
                else
                    dalDataLoader = Activator.CreateInstance(classDataLoader.LoaderType, new object[] { ContextOptions });

                // create a new instance of the DALDataSet<T> and pass the data loader
                dataSet = Activator.CreateInstance(typeof(RelmDataSet<>).MakeGenericType(typeof(T)), new object[] { this, dalDataLoader }) as IRelmDataSet<T>;
                if (dataSet == null)
                    throw new InvalidOperationException($"Failed to create DataSet for type {typeof(T).Name}.");

                attachedProperty.SetValue(this, dataSet);
            }

            return dataSet;
        }

        /// <summary>
        /// Checks if the DALDataSet of a specific type is attached to the current DALContext instance.
        /// </summary>
        /// <typeparam name="T">The type of the dataset, which should inherit from CS_DbModel.</typeparam>
        /// <returns>True if the dataset exists, otherwise false.</returns>
        public bool HasDataSet<T>(bool throwException = true) where T : IRelmModel, new()
        {
            return HasDataSet(typeof(T), throwException: throwException);
        }

        /// <summary>
        /// Checks if the DALDataSet of a specific type is attached to the current DALContext instance.
        /// </summary>
        /// <param name="dataSetType">The System.Type of the dataset to check.</param>
        /// <returns>True if the dataset exists, otherwise false.</returns>
        public bool HasDataSet(Type dataSetType, bool throwException = true)
        {
            return GetDataSetType(dataSetType, throwException: throwException) != null;
        }

        public IRelmDataSet<T> GetDataSetType<T>() where T : IRelmModel, new()
        {
            return GetDataSetType<T>(throwException: true);
        }

        /// <summary>
        /// Gets the dataset of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the dataset, which should inherit from CS_DbModel.</typeparam>
        /// <returns>An instance of IDALDataSet of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no matching dataset is found.</exception>
        public IRelmDataSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new()
        {
            return (IRelmDataSet<T>)GetDataSetType(typeof(T), throwException: throwException);
        }

        public IRelmDataSetBase GetDataSetType(Type dataSetType)
        {
            return GetDataSetType(dataSetType, throwException: true);
        }

        /// <summary>
        /// Gets the dataset of the given type.
        /// </summary>
        /// <param name="dataSetType">Type of the dataset.</param>
        /// <returns>An IDALDataSetBase instance of the given type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no matching dataset is found.</exception>

        public IRelmDataSetBase GetDataSetType(Type dataSetType, bool throwException)
        {
            // Find the first property that is of type DALDataSet<> and has a generic type argument that matches DataSetType
            var dataSetProperty = this.GetType()
                .GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>) &&
                                     p.PropertyType.GetGenericArguments()[0] == dataSetType);

            if (dataSetProperty == null && throwException)
            {
#if DEBUG
                var currentPropertyTypes_DEBUG = this.GetType().GetProperties().Select(x => x.PropertyType).ToList();
                var currentGenericTypes_DEBUG = currentPropertyTypes_DEBUG.Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : default).ToList();
                var currentGenericArguments_DEBUG = currentPropertyTypes_DEBUG.Select(x => x.IsGenericType ? x.GetGenericArguments() : default).ToList();
#endif

                throw new InvalidOperationException($"No RelmDataSet with generic type [{dataSetType.Name}] found in context [{this.GetType().Name}].");
            }

            return dataSetProperty?.GetValue(this) as IRelmDataSetBase;
        }

        public ICollection<T> Get<T>() where T : RelmModel, new()
        {
            var dataSet = GetDataSet<T>()
                ?? throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");

            return dataSet.Load();
        }

        public ICollection<T> Get<T>(Expression<Func<T, bool>> predicate) where T : RelmModel, new()
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate), "Predicate cannot be null.");
            
            var dataSet = GetDataSet<T>()
                ?? throw new InvalidOperationException($"DataSet for type {typeof(T).Name} is not initialized.");

            return dataSet.Where(predicate).Load();
        }
    }
}
