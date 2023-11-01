using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SimpleRelm.Attributes;
using SimpleRelm.Extensions;
using SimpleRelm.Interfaces;
using SimpleRelm.Options;
using SimpleRelm.RelmInternal.Extensions;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
using SimpleRelm.RelmInternal.Helpers.DataTransfer.Persistence;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Models
{
    public class RelmModel : IRelmModel
    {
        [RelmColumn(PrimaryKey: true, Autonumber: true, IsNullable: false)]
        public long? Id { get; set; }

        [RelmColumn(IsNullable: false, DefaultValue: "1")]
        public bool Active { get; set; }

        [RelmKey]
        [RelmDto]
        [RelmColumn(IsNullable: false, Unique: true)]
        public string InternalId { get; set; }

        [RelmColumn(IsNullable: false, DefaultValue: "CURRENT_TIMESTAMP")]
        public DateTime CreateDate { get; set; }

        [RelmColumn(IsNullable: false, DefaultValue: "CURRENT_TIMESTAMP")]
        public DateTime LastUpdated { get; set; }

        /*
        // The regular expression search and replace strings to turn "CapitalCase" property names into "underscore_case" column names
        public static string UnderscoreSearchPattern => @"(?<!_|^|Internal)([A-Z])";
        public static string UnderscoreReplacePattern => @"_$1";
        */

        /// <summary>
        /// Resets every property to its default value
        /// </summary>
        public IRelmModel ResetCoreAttributes(bool NullInternalId = false)
        {
            Active = true;

            if (NullInternalId)
                InternalId = null;
            else
                InternalId = Guid.NewGuid().ToString();

            CreateDate = DateTime.Now;
            LastUpdated = CreateDate;

            return this;
        }

        public RelmModel()
        {
            ResetCoreAttributes();
        }

        public RelmModel(RelmModel FromModel)
        {
            this.Active = FromModel?.Active ?? false;
            this.InternalId = FromModel?.InternalId;
            this.CreateDate = FromModel?.CreateDate ?? default;
            this.LastUpdated = FromModel?.LastUpdated ?? default;
        }

        /// <summary>
        /// Creates an object and automatcially places data from a database row into it based on naming conventions.
        /// </summary>
        /// <param name="ModelData">Row of data from the database.</param>
        /// <param name="AlternateTableName">The alternate table name to search for in data results.</param>
        public RelmModel(DataRow ModelData, string AlternateTableName = null)
        {
            ResetWithData(ModelData, AlternateTableName: AlternateTableName);
        }

        public IRelmModel ResetWithData(DataRow ModelData, string AlternateTableName = null)
        {
            var alternateTableName = AlternateTableName ?? GetType().Name;

            ResetCoreAttributes();

            // match up all properties to columns using underscore names and populate matches with data from the row
            foreach (var underscoreName in GetUnderscoreProperties())
            {
                //TODO: replace Contains(underscoreName.Key) both places below with "IndexOf(underscoreName.Key, StringComparison.InvariantCultureIgnoreCase) >= 0"? Not sure if we care about case.

                // first do the default column names
                if (ModelData.Table.Columns.Contains(underscoreName.Key) && !(ModelData[underscoreName.Key] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                    underscoreName.Value.Item2.SetValue(this, GetValueData(underscoreName.Key, underscoreName.Value.Item2.PropertyType, ModelData));

                // then do the alternate table names
                if (ModelData.Table.Columns.Contains($"{underscoreName.Key}_{alternateTableName}") && !(ModelData[$"{underscoreName.Key}_{alternateTableName}"] is DBNull) && underscoreName.Value.Item2.SetMethod != null)
                    underscoreName.Value.Item2.SetValue(this, GetValueData($"{underscoreName.Key}_{alternateTableName}", underscoreName.Value.Item2.PropertyType, ModelData));
            }

            return this;
        }

        /// <summary>
        /// Gets the data from the named column in the DataRow and properly parses/converts it based on Type factors.
        /// </summary>
        /// <param name="UnderscoreKey">Underscore name of the column.</param>
        /// <param name="PropertyValueType">Type of the property, used for parsing/conversion.</param>
        /// <param name="ModelData">Raw database data row.</param>
        /// <returns>The processed data.</returns>
        private object GetValueData(string UnderscoreKey, Type PropertyValueType, DataRow ModelData)
        {
            object valueData;

            // most primitive types are just 1:1 passthrough and don't require post-processing
            if (PropertyValueType == ModelData[UnderscoreKey].GetType() || ModelData[UnderscoreKey].GetType() == typeof(DateTime))
                valueData = ModelData[UnderscoreKey];

            // if it's an Enum, do a parse
            else if (PropertyValueType.BaseType == typeof(Enum))
                valueData = Enum.Parse(PropertyValueType, ModelData[UnderscoreKey].ToString());

            // if we're putting it in a DateTime, but we have a string, parse it
            else if (PropertyValueType == typeof(DateTime) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = DateTime.TryParse(ModelData[UnderscoreKey].ToString(), out DateTime _dateData) ? _dateData : default;

            // if we're putting it in a TimeSpan, but we have a string, parse it
            else if (PropertyValueType == typeof(TimeSpan) && ModelData[UnderscoreKey].GetType() == typeof(string))
                valueData = TimeSpan.TryParse(ModelData[UnderscoreKey].ToString(), out TimeSpan _timeSpanData) ? _timeSpanData : default;

            // if none of those are true, then we have some serialized JSON data, so deserialize it
            else
                valueData = JsonConvert.DeserializeObject(ModelData[UnderscoreKey].ToString(), PropertyValueType);

            return valueData;
        }

        /// <summary>
        /// Gets the full info about the current object's properties, including the underscore names.
        /// </summary>
        /// <param name="GetOnlyDbResolvables">Indicate to get only properties marked with the DALResolvable attribute.</param>
        /// <returns>The full list of property info including underscore names.</returns>
        public List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(bool GetOnlyDbResolvables = true)
        {
            return UnderscoreNamesHelper.ConvertPropertiesToUnderscoreNames(this.GetType(), GetOnlyDalResolvables: GetOnlyDbResolvables);
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="connectionStringType">Type of connection to use.</param>
        /// <param name="batchSize">The number of items to write out to the database per batch.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(Enum connectionStringType, int batchSize = 100)
        {
            return DataOutputOperations.BulkTableWrite(connectionStringType, this, ForceType: this.GetType(), BatchSize: batchSize);
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="relmContext">An IRelmContext object with open connection and transaction.</param>
        /// <param name="batchSize">The number of items to write out to the database per batch.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(IRelmContext relmContext, int batchSize = 100)
        {
            return DataOutputOperations.BulkTableWrite(relmContext.ContextOptions.DatabaseConnection, this, SqlTransaction: relmContext.ContextOptions.DatabaseTransaction, ForceType: this.GetType(), BatchSize: batchSize);
        }

        /// <summary>
        /// Writes the current object to the database using the table named in the DALTable attribute.
        /// </summary>
        /// <param name="ExistingConnection">An existing and open connection to use when writing this data.</param>
        /// <param name="SqlTransaction">An optional transaction to write to the database under.</param>
        /// <param name="batchSize">The number of items to write out to the database per batch.</param>
        /// <returns>The number of rows written to the database.</returns>
        public int WriteToDatabase(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null, int BatchSize = 100)
        {
            return DataOutputOperations.BulkTableWrite(ExistingConnection, this, SqlTransaction: SqlTransaction, ForceType: this.GetType(), BatchSize: BatchSize);
        }

        public T CopyFromSource<T>(T source) where T : RelmModel, new()
        {
            // create a new object of type T, then run through all the properties and members available on source and copy the value of each property and member that exists on the new object
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            T target = new T();

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Check if the property can be written to and is not an index property
                if (property.CanWrite && property.GetIndexParameters().Length == 0)
                    property.SetValue(target, property.GetValue(source));
            }

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                field.SetValue(target, field.GetValue(source));
            }

            return target;
        }

        /// <summary>
        /// Generate a DTO POCO object based on properties marked with a DALTransferProperty attribute, plus any requested included properties, minus any requested excluded properties.
        /// If no DALTransferProperty attributes are found on a child object, this function will just include all properties from the child object.
        /// </summary>
        /// <param name="IncludeProperties">A list of properties to include in the DTO, even if they aren't marked with DALTransferProperty.</param>
        /// <param name="ExcludeProperties">A list of properties to exclude from the DTO, even if they are marked with DALTransferProperty.</param>
        /// <returns>A serializable object with only the requested properties included.</returns>
        public dynamic GenerateDTO(IEnumerable<string> IncludeProperties = null, IEnumerable<string> ExcludeProperties = null, string SourceObjectName = null, Func<IRelmModel, Dictionary<string, object>> GetAdditionalObjectProperties = null, int Iteration = 0)
        {
            var baseRef = this;

            var namespaceIterations = baseRef
                .GetType()
                .FullName
                .Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            var sourceObjectIterations = SourceObjectName
                ?.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                ??
                Enumerable.Empty<string>();

            // get object properties, if any are DALBaseModels marked with DALTransferProperty then GenerateDTO() on those recursively, otherwise just return the value. if there are any IEnumerables, DTO each item in the enumerable.
            return (ExpandoObject)baseRef
                .GetType()
                .GetRuntimeProperties()
                .Select(x => new KeyValuePair<PropertyInfo, IEnumerable<string>>(x, namespaceIterations
                    .Select((y, index) => string.Join(".", namespaceIterations.Skip(index).Append(x.Name)))
                    .Append(x.Name)
                    .Concat(sourceObjectIterations
                        .Select((y, index) => string.Join(".", sourceObjectIterations.Skip(index).Append(x.Name)))
                        .Append(x.Name))
                    .Where(y => !string.IsNullOrWhiteSpace(y))))
                .Where(x => (x.Key.GetCustomAttribute<RelmDto>() != null
                        || ((IncludeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase)?.Count() ?? 0) > 0))
                    && !((ExcludeProperties?.Intersect(x.Value, StringComparer.InvariantCultureIgnoreCase).Count() ?? 0) > 0))
                .Select(x => x.Key)
                .Aggregate(new ExpandoObject() as IDictionary<string, object>,
                    (seed, property) =>
                    {
                        // look for enumerables, DTO each item within
                        if (property
                                .PropertyType
                                .GetInterfaces()
                                .Intersect(typeof(IEnumerable<>)
                                    .GetInterfaces())
                                .Count()
                                > 0
                            &&
                            property
                                .PropertyType
                                .GenericTypeArguments
                                .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name)
                                    ? null
                                    : new Type[] { x.BaseType })
                                .Contains(typeof(RelmModel)))
                        {
                            seed.Add(property.Name,
                                ((IEnumerable<RelmModel>)property
                                    .GetValue(baseRef))
                                    .Select(x => x.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), GetAdditionalObjectProperties: GetAdditionalObjectProperties, Iteration: Iteration + 1)));
                        }
                        else
                        {
                            /* FOR DEBUGGING
                            var hasTransfer = property
                                    .PropertyType
                                    ?.GetRuntimeProperties()
                                    ?.Any(x => x.GetCustomAttribute<DALTransferProperty>() != null);

                            var isBaseModel = new Type[] { property.PropertyType }
                                    .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                    .Contains(typeof(DALBaseModel));

                            var fieldValue = property.GetValue(baseRef);
                            if ((hasTransfer ?? false) && isBaseModel)
                                fieldValue = ((DALBaseModel)property.GetValue(baseRef))?.GenerateDTO(BaseRef: baseRef, IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))));

                            seed.Add(property.Name, fieldValue);
                            */
                            // convert a property
                            seed.Add(property.Name,
                                (property
                                    .PropertyType
                                    ?.GetRuntimeProperties()
                                    ?.Any(x => x.GetCustomAttribute<RelmDto>() != null)
                                    ??
                                    false)
                                &&
                                new Type[] { property.PropertyType }
                                    .FlattenTreeObject(x => string.IsNullOrWhiteSpace(x?.BaseType?.Name) ? null : new Type[] { x.BaseType })
                                    .Contains(typeof(RelmModel))
                                ? ((RelmModel)property.GetValue(baseRef))?.GenerateDTO(IncludeProperties: IncludeProperties, ExcludeProperties: ExcludeProperties, SourceObjectName: string.Join(".", new List<string> { SourceObjectName, property.Name }.Where(y => !string.IsNullOrWhiteSpace(y))), GetAdditionalObjectProperties: GetAdditionalObjectProperties, Iteration: Iteration + 1)
                                : property.GetValue(baseRef));
                        }

                        if (Iteration == 0 && GetAdditionalObjectProperties != null)
                        {
                            var additionalProperties = GetAdditionalObjectProperties(this)
                                ?.Where(x => !seed.ContainsKey(x.Key) && !(ExcludeProperties?.Contains(x.Key) ?? false))
                                .ToList();

                            if (additionalProperties != null)
                            {
                                foreach (var additionalProperty in additionalProperties)
                                {
                                    seed.Add(additionalProperty);
                                }
                            }
                        }

                        return seed;
                    });
        }

        private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            // base case:
            IEnumerable<IEnumerable<T>> result = new[] { Enumerable.Empty<T>() };
            foreach (var sequence in sequences)
            {
                var s = sequence; // don't close over the loop variable
                                  // recursive case: use SelectMany to build the new product out of the old one
                result =
                    from seq in result
                    from item in s
                    select seq.Concat(new[] { item });
            }
            return result;
        }

        public RelmModel Duplicate()
        {
            return (RelmModel)this.MemberwiseClone();
        }

        /*
        public IRelmModel LoadForeignKeyField<S>(RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<IRelmModel, S>> predicate)
        {
            /*
            //public T LoadForeignKeyField<T, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new()
            return new ForeignKeyLoader<T>(this, relmContextOptionsBuilder)
                .LoadForeignKey(predicate)
                ?.FirstOrDefault();
            * /
            var loaderType = typeof(ForeignKeyLoader<>).MakeGenericType(this.GetType());
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { this, relmContextOptionsBuilder });
            var loaderResult = loaderType.GetMethod(nameof(ForeignKeyLoader<RelmModel>.LoadForeignKey)).Invoke(loaderInstance, new object[] { predicate });

            return ((ICollection<IRelmModel>)loaderResult).FirstOrDefault();
        }

        public IRelmModel LoadDataLoaderField<S>(Expression<Func<IRelmModel, S>> predicate)
        {
            /*
            //public T LoadDataLoaderField<T, S>(Expression<Func<T, S>> predicate) where T : IRelmModel, new()
            return new DataLoaderHelper<T>(this)
                .LoadField(predicate)
                ?.FirstOrDefault();
            * /
            var loaderType = typeof(DataLoaderHelper<>).MakeGenericType(this.GetType());
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { this });
            var loaderResult = loaderType.GetMethod(nameof(DataLoaderHelper<RelmModel>.LoadField)).Invoke(loaderInstance, new object[] { predicate });

            return ((ICollection<IRelmModel>)loaderResult).FirstOrDefault();
        }
        */
    }
}
