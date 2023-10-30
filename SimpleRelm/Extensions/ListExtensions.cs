using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Extensions
{
    public static class ListExtensions
    {
        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, Enum ConnectionStringType, string TableName = null, Type ForceType = null, bool AllowUserVariables = false, int BatchSize = 100, string DatabaseName = null) where T : IRelmModel
        {
            return DataOutputOperations.BulkTableWrite<T>(ConnectionStringType, DbModelData, TableName, ForceType, AllowUserVariables, BatchSize, DatabaseName);
        }

        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null, string TableName = null, Type ForceType = null, bool AllowUserVariables = false, int BatchSize = 100, string DatabaseName = null) where T : IRelmModel
        {
            return DataOutputOperations.BulkTableWrite<T>(ExistingConnection, DbModelData, TableName, SqlTransaction: SqlTransaction, ForceType, BatchSize, DatabaseName);
        }

        public static int WriteToDatabase<T>(this IEnumerable<T> DbModelData, IRelmContext relmContext, string TableName = null, Type ForceType = null, bool AllowUserVariables = false, int BatchSize = 100, string DatabaseName = null)
        {
            return DataOutputOperations.BulkTableWrite<T>(relmContext.ContextOptions.DatabaseConnection, DbModelData, TableName, SqlTransaction: relmContext.ContextOptions.DatabaseTransaction, ForceType, BatchSize, DatabaseName);
        }

        public static T LoadForeignKeys<T>(this IEnumerable<T> DbModelData, Expression<Func<T, object>> predicate) where T : IRelmModel
        {
            return new ForeignKeyLoader<IEnumerable<T>>(DbModelData).LoadMyForeignKey(predicate);
        }

        public static ICollection<T> FlattenTreeObject<T>(this IEnumerable<T> EnumerableList, Func<T, ICollection<T>> GetChildrenFunction)
        {
            return EnumerableList
                .SelectMany(enumerableItem =>
                    Enumerable
                    .Repeat(enumerableItem, 1)
                    .Concat(GetChildrenFunction(enumerableItem)
                        ?.FlattenTreeObject(GetChildrenFunction)
                        ??
                        Enumerable.Empty<T>()))
                .ToList();
        }

        public static ICollection<dynamic> GenerateDTO<T>(this IEnumerable<T> BaseObjects, ICollection<string> IncludeProperties = null, ICollection<string> ExcludeProperties = null) where T : IRelmModel
        {
            return BaseObjects.Select(x => x.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties)).ToList();
        }

        public static KeyValuePair<TKey, TValue> GetEntry<TKey, TValue>
            (this IDictionary<TKey, TValue> dictionary,
             TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, dictionary[key]);
        }
    }
}
