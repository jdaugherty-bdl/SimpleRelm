using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Models
{
    public class RelmContext : IDisposable, IRelmContext
    {
        public RelmContextOptionsBuilder ContextOptions { get; private set; }

        private IEnumerable<PropertyInfo> _attachedProperties;
        private List<object> _attachedDataSets;

        public RelmContext(RelmContextOptionsBuilder optionsBuilder, bool autoOpenConnection = true, bool autoOpenTransaction = false)
        {
            ContextOptions = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder), "RelmContextOptionsBuilder cannot be null.");

            ContextOptions.ValidateAllSettings();

            InitializeContext(autoOpenConnection, autoOpenTransaction);
        }

        public RelmContext(string connectionDetails, bool autoOpenConnection = true, bool autoOpenTransaction = false)
        {
            // set the options and allow user to override
            ContextOptions = new RelmContextOptionsBuilder(connectionDetails);

            InitializeContext(autoOpenConnection, autoOpenTransaction);
        }

        public RelmContext(MySqlConnection connection, bool autoOpenConnection = true, bool autoOpenTransaction = false)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection);

            InitializeContext(autoOpenConnection, autoOpenTransaction);
        }

        public RelmContext(MySqlConnection connection, MySqlTransaction transaction, bool autoOpenConnection = true)
        {
            ContextOptions = new RelmContextOptionsBuilder(connection, transaction);

            InitializeContext(autoOpenConnection, false);
        }

        private void InitializeContext(bool autoOpenConnection = true, bool autoOpenTransaction = false)
        {
            if (autoOpenConnection)
            {
                if (ContextOptions.DatabaseConnection == null)
                    ContextOptions.SetDatabaseConnection(RelmHelper.GetConnectionFromString(ContextOptions.DatabaseConnectionString));

                if (ContextOptions.DatabaseConnection != null)
                    StartConnection(autoOpenTransaction);
            }

            _attachedDataSets = new List<object>();

            // call the user's OnConfigure method
            OnConfigure(ContextOptions);

            InitializeDataSets();
        }

        private void InitializeDataSets()
        {
            // find any properties that are DALDataSet<T>
            _attachedProperties = this.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(IRelmDataSet<>));

            // instantiate each item in the DALDataSet<T> properties
            foreach (var attachedProperty in _attachedProperties)
            {
                var dalDataSetType = attachedProperty.PropertyType.GetGenericArguments()[0];

                var dalDataSet = Activator.CreateInstance(typeof(RelmDataSet<>).MakeGenericType(dalDataSetType), new object[] { this });

                attachedProperty.SetValue(this, dalDataSet);

                _attachedDataSets.Add(dalDataSet);
            }
        }

        public virtual void OnConfigure(RelmContextOptionsBuilder OptionsBuilder) { }

        public void StartConnection(bool autoOpenTransaction = false)
        {
            if (ContextOptions.DatabaseConnection == null)
                throw new InvalidOperationException("Cannot open a non-existent database connection.");

            if (ContextOptions.DatabaseConnection.State == System.Data.ConnectionState.Closed)
                ContextOptions.DatabaseConnection.Open();

            if (autoOpenTransaction && ContextOptions.DatabaseConnection.State == System.Data.ConnectionState.Open)
                ContextOptions.SetDatabaseTransaction(ContextOptions.DatabaseConnection.BeginTransaction());
        }

        public void EndConnection(bool commitTransaction = true)
        {
            if (ContextOptions.DatabaseConnection?.State != System.Data.ConnectionState.Closed)
            {
                if (commitTransaction)
                    ContextOptions.DatabaseTransaction?.Commit();

                ContextOptions.DatabaseConnection.Close();
            }
        }

        public MySqlTransaction BeginTransaction()
        {
            if (ContextOptions.DatabaseTransaction == null)
                ContextOptions.SetDatabaseTransaction(ContextOptions.DatabaseConnection?.BeginTransaction());

            return ContextOptions.DatabaseTransaction;
        }

        public void CommitTransaction()
        {
            ContextOptions.DatabaseTransaction?.Commit();

            ContextOptions.SetDatabaseTransaction(null);
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

                _attachedDataSets.Clear();
            }
        }

        ~RelmContext()
        {
            Dispose(false);
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
                throw new InvalidOperationException($"No DALDataSet property with type {dataSetType.Name} found on the current object.");
            }

            return dataSetProperty?.GetValue(this) as IRelmDataSetBase;
        }

        public void SaveAll()
        {
            // loop through each _attachedDataSet and call Save()
            foreach (var attachedDataSet in _attachedDataSets)
            {
                var saveMethod = attachedDataSet.GetType().GetMethod(nameof(RelmDataSet<RelmModel>.Save));

                saveMethod.Invoke(attachedDataSet, null);
            }
        }
    }
}
