using MySql.Data.MySqlClient;
using SimpleRelm.Models;
using SimpleRelm.Options;
using SimpleRelm.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces.RelmQuick
{
    /// <summary>
    /// Defines a context for interacting with a Relm database, providing methods for managing connections, 
    /// transactions, and data operations. This interface supports querying, data manipulation, and bulk operations 
    /// while ensuring proper resource management.
    /// </summary>
    /// <remarks>Implementations of this interface are designed to facilitate database operations in a
    /// structured and  transactional manner. The context provides methods for starting and ending connections, managing
    /// transactions,  and performing CRUD operations on data sets. It also includes support for executing raw queries
    /// and bulk  operations. The interface extends <see cref="IDisposable"/> to ensure that resources are properly
    /// released  when the context is no longer needed.  Thread safety is not guaranteed unless explicitly stated by the
    /// implementation. Users should ensure proper  synchronization when accessing the context from multiple
    /// threads.</remarks>
    public interface IRelmQuickContext : IDisposable
    {
        /// <summary>
        /// Gets the builder used to configure options for the current Relm context.
        /// </summary>
        RelmContextOptionsBuilder ContextOptions { get; }

        /// <summary>
        /// Commits the current transaction, making all changes permanent in the database.
        /// </summary>
        void CommitTransaction();
        
        /// <summary>
        /// Rolls back the current transaction, undoing any changes made since the transaction began.
        /// </summary>
        /// <remarks>This method should be called to revert changes if an error occurs or if the
        /// transaction cannot be completed successfully.  Ensure that a transaction is active before calling this
        /// method; otherwise, an exception may be thrown.</remarks>
        void RollbackTransaction();

        /// <summary>
        /// Rolls back the current transaction, undoing any changes made since the transaction began.
        /// </summary>
        /// <remarks>This method should be called to revert changes if an error occurs or if the
        /// transaction cannot be completed successfully.  Ensure that a transaction is active before calling this
        /// method; otherwise, an exception may be thrown.</remarks>
        void RollbackTransactions();

        /// <summary>
        /// Configures the data loader for the specified model type.
        /// </summary>
        /// <typeparam name="T">The type of the model that the data loader will handle. Must inherit from <see cref="RelmModel"/> and have a
        /// parameterless constructor.</typeparam>
        /// <param name="dataLoader">The data loader instance to associate with the specified model type. Cannot be <see langword="null"/>.</param>
        void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new();

        /// <summary>
        /// Starts a connection to the database.
        /// </summary>
        /// <remarks>If <paramref name="autoOpenTransaction"/> is set to <see langword="true"/>, ensure
        /// that the transaction  is committed or rolled back to avoid leaving it open. This method must be called
        /// before performing any  database operations.</remarks>
        /// <param name="autoOpenTransaction">Specifies whether a transaction should be automatically opened after the connection is established.  Pass
        /// <see langword="true"/> to open a transaction automatically; otherwise, <see langword="false"/>.</param>
        void StartConnection(bool autoOpenTransaction = false);

        /// <summary>
        /// Ends the current connection and optionally commits any active transaction.
        /// </summary>
        /// <remarks>Use this method to cleanly terminate a connection. If a transaction is active, you
        /// can  specify whether to commit or roll it back before the connection is closed. Ensure that  any necessary
        /// operations are completed before calling this method, as the connection  will no longer be available
        /// afterward.</remarks>
        /// <param name="commitTransaction">A value indicating whether to commit the active transaction before ending the connection.  <see
        /// langword="true"/> to commit the transaction; <see langword="false"/> to roll it back.  The default is <see
        /// langword="true"/>.</param>
        void EndConnection(bool commitTransaction = true);
        
        /// <summary>
        /// Determines whether a dataset of the specified type exists in the current context.
        /// </summary>
        /// <typeparam name="T">The type of the dataset to check for. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="throwException">A value indicating whether to throw an exception if the dataset does not exist.  <see langword="true"/> to
        /// throw an exception; <see langword="false"/> to return <see langword="false"/> instead.</param>
        /// <returns><see langword="true"/> if the dataset of the specified type exists; otherwise, <see langword="false"/>.</returns>
        bool HasDataSet<T>(bool throwException = true) where T : IRelmModel, new();
        
        /// <summary>
        /// Determines whether a dataset of the specified type is available.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to check for availability. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the dataset is not available. If <see
        /// langword="true"/>, an exception is thrown when the dataset is not found; otherwise, the method returns <see
        /// langword="false"/>.</param>
        /// <returns><see langword="true"/> if the dataset of the specified type is available; otherwise, <see
        /// langword="false"/>.</returns>
        bool HasDataSet(Type dataSetType, bool throwException = true);

        /// <summary>
        /// Retrieves an initialized instance of a dataset of the specified type.
        /// </summary>
        /// <remarks>Use this method to access a dataset for a specific model type. The type parameter
        /// <typeparamref name="T"/> must represent a model that conforms to the <see cref="IRelmModel"/>
        /// interface.</remarks>
        /// <typeparam name="T">The type of the dataset to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <returns>An instance of <see cref="IRelmDataSet{T}"/> containing the data for the specified type.</returns>
        IRelmDataSet<T> GetDataSet<T>() where T : IRelmModel, new();

        /// <summary>
        /// Retrieves an initialized instance of a dataset of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the dataset to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the dataset cannot be retrieved. If <see
        /// langword="true"/>, an exception is thrown on failure; otherwise, <see langword="null"/> is returned.</param>
        /// <returns>An instance of <see cref="IRelmDataSet{T}"/> containing the dataset of type <typeparamref name="T"/>.
        /// Returns <see langword="null"/> if the dataset cannot be retrieved and <paramref name="throwException"/> is
        /// <see langword="false"/>.</returns>
        IRelmDataSet<T> GetDataSet<T>(bool throwException) where T : IRelmModel, new();
        
        /// <summary>
        /// Retrieves an initialized instance of a dataset based on the specified type.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to retrieve. This must be a type that implements <see
        /// cref="IRelmDataSetBase"/>.</param>
        /// <returns>An instance of the dataset that matches the specified type. Returns <see langword="null"/> if no matching
        /// dataset is found.</returns>
        IRelmDataSetBase GetDataSet(Type dataSetType);

        /// <summary>
        /// Retrieves an initialized instance of a dataset of the specified type.
        /// </summary>
        /// <param name="dataSetType">The <see cref="Type"/> of the dataset to retrieve. This must implement <see cref="IRelmDataSetBase"/>.</param>
        /// <param name="throwException">A value indicating whether an exception should be thrown if the dataset cannot be found.  If <see
        /// langword="true"/>, an exception is thrown when the dataset is not found; otherwise, <see langword="null"/>
        /// is returned.</param>
        /// <returns>An instance of <see cref="IRelmDataSetBase"/> representing the requested dataset, or <see langword="null"/>
        /// if the dataset is not found and <paramref name="throwException"/> is <see langword="false"/>.</returns>
        IRelmDataSetBase GetDataSet(Type dataSetType, bool throwException);
        
        /// <summary>
        /// Retrieves a dataset of the specified type.
        /// </summary>
        /// <remarks>This method is generic and allows retrieval of datasets for any type that satisfies
        /// the constraints.</remarks>
        /// <typeparam name="T">The type of the dataset to retrieve. Must implement <see cref="IRelmModel"/> and have a parameterless
        /// constructor.</typeparam>
        /// <returns>An instance of <see cref="IRelmDataSet{T}"/> representing the dataset of the specified type.</returns>
        IRelmDataSet<T> GetDataSetType<T>() where T : IRelmModel, new();
        IRelmDataSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new();
        IRelmDataSetBase GetDataSetType(Type dataSetType);
        IRelmDataSetBase GetDataSetType(Type dataSetType, bool throwException);
        ICollection<T> Get<T>(bool loadDataLoaders = false) where T : IRelmModel, new();
        ICollection<T> Get<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new();
        T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, bool loadDataLoaders = false) where T : IRelmModel, new();
        IRelmDataSet<T> Where<T>(Expression<Func<T, bool>> predicate) where T : IRelmModel, new();
        ICollection<T> Run<T>(string query, Dictionary<string, object> parameters = null) where T : IRelmModel, new();

        string GetLastInsertId();
        string GetIdFromInternalId(string Table, string InternalId);
        DataRow GetDataRow(string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true);
        DataTable GetDataTable(string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true);
        T GetDataObject<T>(string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true) where T : IRelmModel, new();
        IEnumerable<T> GetDataObjects<T>(string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true) where T : IRelmModel, new();
        IEnumerable<T> GetDataList<T>(string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true);
        T GetScalar<T>(string query, Dictionary<string, object> parameters = null, bool throwException = true);

        BulkTableWriter<T> GetBulkTableWriter<T>(string InsertQuery = null, bool UseTransaction = false, bool ThrowException = true, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false);
        int BulkTableWrite<T>(T SourceData, string TableName = null, MySqlTransaction SqlTransaction = null, Type ForceType = null, int BatchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false);

        void DoDatabaseWork(string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false);
        T DoDatabaseWork<T>(string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool UseTransaction = false);
        void DoDatabaseWork(string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false);
        T DoDatabaseWork<T>(string QueryString, Func<MySqlCommand, object> ActionCallback, bool ThrowException = true, bool UseTransaction = false);

        int WriteToDatabase(IRelmModel relmModel, int batchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false);
        int WriteToDatabase(IEnumerable<IRelmModel> relmModels, int batchSize = 100, bool AllowAutoIncrementColumns = false, bool AllowPrimaryKeyColumns = false, bool AllowUniqueColumns = false, bool AllowAutoDateColumns = false);
    }
}
