using MySql.Data.MySqlClient;
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
    public interface IRelmQuickContext : IDisposable
    {
        RelmContextOptionsBuilder ContextOptions { get; }

        void CommitTransaction();
        void RollbackTransactions();

        void StartConnection(bool autoOpenTransaction = false);
        void EndConnection(bool commitTransaction = true);
        bool HasDataSet<T>(bool throwException = true) where T : IRelmModel, new();
        bool HasDataSet(Type dataSetType, bool throwException = true);
        IRelmDataSet<T> GetDataSet<T>() where T : IRelmModel, new();
        IRelmDataSet<T> GetDataSet<T>(bool throwException) where T : IRelmModel, new();
        IRelmDataSetBase GetDataSet(Type dataSetType);
        IRelmDataSetBase GetDataSet(Type dataSetType, bool throwException);
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
    }
}
