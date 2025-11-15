using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.Options;
using SimpleRelm.Persistence;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces
{
    public interface IRelmContext
    {
        RelmContextOptionsBuilder ContextOptions { get; }

        void SetDataLoader<T>(IRelmDataLoader<T> dataLoader) where T : RelmModel, new();

        IRelmDataSet<T> GetDataSetType<T>() where T : IRelmModel, new();
        IRelmDataSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new();
        IRelmDataSetBase GetDataSetType(Type dataSetType);
        IRelmDataSetBase GetDataSetType(Type dataSetType, bool throwException);

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

        void CommitTransaction();
        void RollbackTransactions();
    }
}
