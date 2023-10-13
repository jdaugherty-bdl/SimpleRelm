using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Extensions;
using SimpleRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.Models
{
    public class RelmDataSet<T> : ICollection<T>, IRelmDataSet<T> where T : RelmModel, new()
    {
        public bool Modified { get; set; }

        public int Count => _items?.Count ?? 0;
        public bool IsReadOnly => _items?.IsReadOnly ?? true;

        private readonly string TableName;
        private readonly string FullPropertySelectList;
        private readonly Dictionary<string, string> UnderscoreProperties;
        private readonly IRelmContext CurrentContext;

        private Dictionary<Command, List<Expression>> _commands;
        private Dictionary<Command, List<Expression>> _lastCommandsExecuted;
        private ICollection<T> _items;

        public RelmDataSet(IRelmContext currentContext)
        {
            CurrentContext = currentContext ?? throw new ArgumentNullException(nameof(currentContext));

            Modified = false;

            // get the table name from the DALTable attribute of T
            TableName = typeof(T).GetCustomAttribute<RelmTable>(false).TableName;

            if (string.IsNullOrWhiteSpace(TableName))
                throw new Exception($"DALTable attribute not found on type {nameof(T)}");

            // get a list of all properties on T that are marked with the DALResolvable attribute
            UnderscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true).ToDictionary(x => x.Value.Item1, x => x.Key);

            // get a list of all class property names surrounded by ` quotes separated by commas
            FullPropertySelectList = string.Join(", ", UnderscoreProperties.Select(p => $"a.`{p.Value}`"));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (_items ?? Load()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<Expression> PrewarmQuery(Command PredicateCommand)
        {
            if (_commands == null)
                _commands = new Dictionary<Command, List<Expression>>();

            if (!_commands.ContainsKey(PredicateCommand))
                _commands.Add(PredicateCommand, new List<Expression>());

            return _commands[PredicateCommand];
        }

        public IRelmDataSet<T> Where(Expression<Func<T, bool>> predicate)
        {
            PrewarmQuery(Command.Where)
                .Add(predicate);

            return this;
        }

        public IRelmDataSet<T> Reference(Expression<Func<T, object>> predicate)
        {
            PrewarmQuery(Command.Reference)
                .Add(predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Collection(Expression<Func<T, object>> predicate)
        {
            PrewarmQuery(Command.Collection)
                .Add(predicate.Body);

            return this;
        }

        public T Find(int ItemId)
        {
            return Where(x => x.Id == ItemId).FirstOrDefault();
        }

        public T Find(string ItemInternalId)
        {
            return Where(x => x.InternalId == ItemInternalId).FirstOrDefault();
        }

        public T FirstOrDefault()
        {
            return FirstOrDefault(null, true);
        }

        public T FirstOrDefault(bool loadItems)
        {
            return FirstOrDefault(null, loadItems);
        }

        public T FirstOrDefault(Expression<Func<T, bool>> predicate)
        {
            return FirstOrDefault(predicate, true);
        }

        public T FirstOrDefault(Expression<Func<T, bool>> predicate, bool loadItems)
        {
            if (loadItems)
            {
                Limit(1);

                if (predicate != null)
                    Where(predicate);

                _items = Load();
            }

            return _items.FirstOrDefault();
        }

        public ICollection<T> Load()
        {
            _items = GetLoadData();

            if (_items?.Any() ?? false)
            {
                // load all references
                if (_lastCommandsExecuted.ContainsKey(Command.Reference))
                    LoadReference();

                // load all collections
                if (_lastCommandsExecuted.ContainsKey(Command.Collection))
                    LoadCollection();
            }

            return _items;
        }

        internal ICollection<T> GetLoadData()
        {
            var findOptions = new Dictionary<string, object>();
            var selectQuery = GetSelectQuery(findOptions);

            if (CurrentContext.ContextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return RelmHelper.GetDataObjects<T>(CurrentContext.ContextOptions.DatabaseConnection, selectQuery, findOptions, SqlTransaction: CurrentContext.ContextOptions.DatabaseTransaction).ToList();
            else
                return RelmHelper.GetDataObjects<T>(CurrentContext.ContextOptions.ConnectionStringType, selectQuery, findOptions).ToList();
        }

        public int Write()
        {
            var findOptions = new Dictionary<string, object>();

            var selectQuery = GetUpdateQuery(findOptions);

            if (CurrentContext.ContextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return RelmHelper.DoDatabaseWork<int>(CurrentContext.ContextOptions.DatabaseConnection, selectQuery, findOptions, SqlTransaction: CurrentContext.ContextOptions.DatabaseTransaction);
            else
                return RelmHelper.DoDatabaseWork<int>(CurrentContext.ContextOptions.ConnectionStringType, selectQuery, findOptions);
        }

        /// <summary>
        /// Loads related single objects (references) into the current data set based on foreign key attributes.
        /// </summary>
        /// <remarks>
        /// Uses reflection to dynamically generate the queries and collect the data for these references.
        /// This method assumes that each "reference" refers to a property that is a single object (e.g., not a collection).
        /// 
        /// The process involves the following steps:
        /// - Validate that the property representing the reference conforms to expected types.
        /// - Locate a property in the related type that is marked with the DALForeignKey attribute, which indicates a foreign key relationship.
        /// - Generate a WHERE clause based on the foreign key relationship to identify the specific object.
        /// - Execute the query and fill the property in the current data set with the loaded object.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if any of the validations or assumptions fail.</exception>
        /// <exception cref="Exception">General exception for unexpected issues, such as a failure to find attributes or properties.</exception>
        private void LoadReference()
        {
            foreach (var reference in _lastCommandsExecuted[Command.Reference])
            {
                LoadForeignObjects(reference);
            }
        }

        /// <summary>
        /// Loads related collections into the current data set based on foreign key attributes.
        /// </summary>
        /// <remarks>
        /// Uses reflection to dynamically generate the queries and collect the data for these collections.
        /// This method assumes that each "collection" refers to a property that is a collection (e.g., ICollection<T>).
        /// 
        /// The process involves the following steps:
        /// - Validate that the property representing the collection conforms to expected types.
        /// - Locate a property in the related type that is marked with the DALForeignKey attribute, which indicates a foreign key relationship.
        /// - Generate a WHERE clause based on the foreign key relationship to filter the collection.
        /// - Execute the query and fill the property in the current data set with the loaded items.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if any of the validations or assumptions fail.</exception>
        /// <exception cref="Exception">General exception for unexpected issues, such as a failure to find attributes or properties.</exception>
        private void LoadCollection()
        {
            foreach (var collection in _lastCommandsExecuted[Command.Collection])
            {
                LoadForeignObjects(collection);
            }
        }

        private void LoadForeignObjects(Expression collection)
        {
            var referenceProperty = collection as MemberExpression
                ?? throw new InvalidOperationException("Collection must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var referenceType = referenceProperty.Type;
            var isCollection = referenceType.IsGenericType && referenceType.GetGenericTypeDefinition() == typeof(ICollection<>);

            // The type of class being referenced by the collection command
            var genericTypeArgument = referenceType;
            if (isCollection)
            {
                genericTypeArgument = referenceType.GetGenericArguments()[0];

                // Check if the referenceType is compatible with ICollection<>
                if (!typeof(ICollection<>).MakeGenericType(genericTypeArgument).IsAssignableFrom(referenceType))
                    throw new InvalidOperationException($"Reference property type must be compatible with ICollection<{genericTypeArgument}>.");
            }

            // Find the DALForeignKey attribute on the current item's property
            var foreignKeyAttribute = referenceProperty.Member.GetCustomAttribute<RelmForeignKey>()
                ?? throw new InvalidOperationException("RelmForeignKey attribute not found on reference property.");

            // Find the property on the generic type that has a DALForeignKey attribute, 
            // and if the property is a generic, look at the generic type argument for the DALForeignKey attribute
            var foreignKeyProperty = genericTypeArgument.GetProperties()
                .Where(x =>
                    Attribute.IsDefined(x, typeof(RelmForeignKey)) ||
                    (x.PropertyType.IsGenericType &&
                    x.PropertyType.GetGenericArguments().Any(y => Attribute.IsDefined(y, typeof(RelmForeignKey))))
                )
                .FirstOrDefault(x => x.PropertyType == typeof(T) ||
                                     (x.PropertyType.IsGenericType &&
                                      x.PropertyType.GetGenericArguments().Any(y => y == typeof(T))));

            if (foreignKeyProperty == null)
            {
                throw new Exception("No property found with DALForeignKey attribute on the related entity.");
            }

            // Get the DALForeignKey attribute from the matching property
            var dalForeignKey = (((RelmForeignKey)Attribute.GetCustomAttribute(foreignKeyProperty, typeof(RelmForeignKey)))?.ForeignKey)
                ?? throw new Exception("DALForeignKey attribute not found on related entity.");

            // Generate a Func<> type based on the generic type argument for use below
            var funcType = typeof(Func<,>).MakeGenericType(genericTypeArgument, typeof(bool));

            // get the property named by dalForeignKey from the type defined in genericTypeArgument and create a MemberExpression from it
            var parameter = Expression.Parameter(genericTypeArgument, "x");
            var memberExpression = Expression.Property(parameter, dalForeignKey)
                ?? throw new Exception("Property referenced by DALForeignKey attribute could not be found.");

            // Instantiate a new DALContext of the same type as CurrentContext so we can load the data we need without modifying anything in our context
            var newDalContextType = CurrentContext.GetType();

            // Find the DALDataSet with the same generic type as genericTypeArgument and create a new one
            var dataSetMethod = newDalContextType.GetMethod(nameof(CurrentContext.GetDataSetType), new[] { typeof(Type) })
                ?? throw new InvalidOperationException("Method not found.");

            var dataSet = dataSetMethod.Invoke(CurrentContext, new object[] { genericTypeArgument }) as IRelmDataSetBase
                ?? throw new InvalidOperationException($"DALDataSet with generic type {genericTypeArgument.Name} not found.");

            // get all the foreign keys to look up
            var itemForeignKeys = _items
                .Select(x => x.GetType().GetProperty(foreignKeyAttribute.ForeignKey)?.GetValue(x))
                .Distinct()
                .ToList();

            // look up the Contains method on the itemForeignKeys type, then make a generic method with the memberExpression type
            var containsMethod = itemForeignKeys
                .GetType()
                .GetMethod(nameof(List<object>.Contains));


            // Get the "Where" method from the data set
            var whereMethod = dataSet
                .GetType()
                .GetMethods()
                .Where(m => m.Name == nameof(RelmDataSet<T>.Where))
                .First();

            // Apply the "Where" and "Load" methods
            var containsExpression = Expression.Call(Expression.Constant(itemForeignKeys), containsMethod, memberExpression);
            var filteredDataSetContains = whereMethod.Invoke(dataSet, new object[] { Expression.Lambda(funcType, containsExpression, parameter) });
            var collectionItemsContains = dataSet.GetType().GetMethod(nameof(Load)).Invoke(filteredDataSetContains, null);

            // use a foreach loop to convert collectionItemsContains to a dictionary where the key is the foreign key and the object is the item
            var collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object), genericTypeArgument));
            foreach (var item in (IEnumerable)dataSet)
            {
                collectionItems.Add(genericTypeArgument.GetProperty(dalForeignKey).GetValue(item), item);
            }

            // loop through each item in _items and add the related item to the collection
            foreach (var item in _items)
            {
                var foreignKeyValue = item.GetType().GetProperty(foreignKeyAttribute.ForeignKey).GetValue(item);
                var collectionItem = collectionItems[foreignKeyValue];
                var collectionProperty = referenceProperty.Member as PropertyInfo;

                if (isCollection)
                {
                    var collectionValue = collectionProperty.GetValue(item);
                    var methodInfo = collectionValue.GetType().GetMethod(nameof(Dictionary<object, object>.Add));

                    methodInfo.Invoke(collectionValue, new object[] { collectionItem });
                }
                else
                    collectionProperty.SetValue(item, collectionItem);
            }
        }

        public IRelmDataSet<T> Entry(T Item)
        {
            if (_items == null)
                Add(Item);
            else
                _items = new List<T> { Item };

            Modified = true;

            return this;
        }

        public IRelmDataSet<T> Entry(T Item, bool Persist = true)
        {
            if (_items == null)
                Add(Item, Persist);
            else
                _items = new List<T> { Item };

            Modified = true;

            return this;
        }


        public IRelmDataSet<T> OrderBy(Expression<Func<T, object>> predicate)
        {
            AddSingleExpression(PrewarmQuery(Command.OrderBy), predicate.Body);

            return this;
        }

        public IRelmDataSet<T> OrderByDescending(Expression<Func<T, object>> predicate)
        {
            AddSingleExpression(PrewarmQuery(Command.OrderByDescending), predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Set(Expression<Func<T, T>> predicate)
        {
            PrewarmQuery(Command.Set)
                .Add(predicate.Body);

            return this;
        }

        public IRelmDataSet<T> GroupBy(Expression<Func<T, object>> predicate)
        {
            AddSingleExpression(PrewarmQuery(Command.GroupBy), predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Limit(int LimitCount)
        {
            AddSingleExpression(PrewarmQuery(Command.Limit), Expression.Constant(LimitCount, LimitCount.GetType()));

            return this;
        }

        private void AddSingleExpression(List<Expression> expressions, Expression expression)
        {
            if (expressions.Count == 0)
                expressions.Add(null);

            expressions[0] = expression;
        }

        public IRelmDataSet<T> DistinctBy(Expression<Func<T, object>> predicate)
        {
            AddSingleExpression(PrewarmQuery(Command.DistinctBy), predicate.Body);

            return this;
        }

        public T Save(T Item)
        {
            // check if the item is already in the list, and if so, replace it, otherwise, add it
            if (_items?.Any(x => x.InternalId == Item.InternalId) ?? false)
            {
                _items = _items.Select(x => x.InternalId == Item.InternalId ? Item : x).ToList();

                Save();
            }
            else
                Add(Item, Persist: true);

            return Item;
        }

        public int Save()
        {
            int rowsUpdated;
            if (CurrentContext.ContextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                rowsUpdated = _items.WriteToDatabase(CurrentContext.ContextOptions.DatabaseConnection, SqlTransaction: CurrentContext.ContextOptions.DatabaseTransaction);
            else
                rowsUpdated = _items.WriteToDatabase(CurrentContext.ContextOptions.ConnectionStringType);

            Modified = false;

            return rowsUpdated;
        }

        public T New()
        {
            return New(null);
        }

        public T New(dynamic NewObjectParameters, bool Persist = true)
        {
            // create a new instance of T
            var newObject = new T();

            // run through each property in the dynamic object, and if the name matches one of the keys in Underscore properties, use reflection to set the value of the new object
            if (NewObjectParameters != null)
                foreach (var property in new RouteValueDictionary(NewObjectParameters))
                    if (UnderscoreProperties.ContainsKey(property.Key))
                        typeof(T).GetProperty(property.Key).SetValue(newObject, property.Value);

            Add(newObject, Persist: Persist);

            return newObject;
        }

        private string GetSelectQuery(Dictionary<string, object> FindOptions)
        {
            return BuildQuery($"SELECT {FullPropertySelectList} ", FindOptions, true);
        }

        private string GetUpdateQuery(Dictionary<string, object> FindOptions)
        {
            return BuildQuery($"UPDATE ", FindOptions, false);
        }

        private string BuildQuery(string QueryPredicate, Dictionary<string, object> FindOptions, bool isSelect)
        {
            // hardcode first table alias to 'a', and inject that into the expression evaluator
            var expressionEvaluator = new ExpressionEvaluator(TableName, UnderscoreProperties, UsedTableAliases: new Dictionary<string, string> { [TableName] = "a" });

            // evaluate all the pieces of the query
            var queryPieces = new Dictionary<Command, List<string>>();
            foreach (var command in _commands)
            {
                if (!queryPieces.ContainsKey(command.Key))
                    queryPieces.Add(command.Key, new List<string>());

                // evaluate all expressions, except references and collections as those are evaluated after selection
                switch (command.Key)
                {
                    case Command.Where:
                        queryPieces[command.Key].Add(expressionEvaluator.EvaluateWhere(command, FindOptions));
                        break;
                    case Command.OrderBy:
                    case Command.OrderByDescending:
                        queryPieces[command.Key].Add(expressionEvaluator.EvaluateOrderBy(command, command.Key == Command.OrderByDescending));
                        break;
                    case Command.Set:
                        queryPieces[command.Key].Add(expressionEvaluator.EvaluateSet(command, FindOptions));
                        break;
                    case Command.Limit:
                        queryPieces[command.Key].Add(expressionEvaluator.EvaluateLimit(command));
                        break;
                    case Command.GroupBy:
                        queryPieces[command.Key].Add(expressionEvaluator.EvaluateGroupBy(command, FindOptions));
                        break;
                    case Command.DistinctBy:
                        queryPieces[command.Key].Add(expressionEvaluator.EvaluateDistinctBy(command));
                        break;
                }
            }

            // build the query
            var predicatePieces = QueryPredicate.Split(' ');
            var findQuery = predicatePieces[0];

            findQuery += " ";

            if (queryPieces.ContainsKey(Command.DistinctBy))
            {
                findQuery += string.Join("\n", queryPieces[Command.DistinctBy]);
                findQuery += ", ";
            }

            if (predicatePieces.Length > 1)
                findQuery += string.Join(" ", predicatePieces.Skip(1));
            if (isSelect)
                findQuery += " FROM ";
            findQuery += $" `{TableName}` a "; // hardcode first table alias to 'a'

            if (queryPieces.ContainsKey(Command.Reference))
                findQuery += string.Join("\n", queryPieces[Command.Reference]);
            if (queryPieces.ContainsKey(Command.Collection))
                findQuery += string.Join("\n", queryPieces[Command.Collection]);

            if (queryPieces.ContainsKey(Command.Where))
                findQuery += string.Join("\n", queryPieces[Command.Where]);

            if (queryPieces.ContainsKey(Command.Set))
                findQuery += string.Join("\n", queryPieces[Command.Set]);

            if (queryPieces.ContainsKey(Command.OrderBy))
                findQuery += string.Join("\n", queryPieces[Command.OrderBy]);
            if (queryPieces.ContainsKey(Command.OrderByDescending))
                findQuery += string.Join("\n", queryPieces[Command.OrderByDescending]);
            if (queryPieces.ContainsKey(Command.GroupBy))
                findQuery += string.Join("\n", queryPieces[Command.GroupBy]);

            if (queryPieces.ContainsKey(Command.Limit))
                findQuery += string.Join("\n", queryPieces[Command.Limit]);

            _lastCommandsExecuted = _commands;
            _commands = null;

            findQuery += ";";

            return findQuery;
        }

        public void Add(T item)
        {
            Add(item, true);
        }

        public void Add(T item, bool Persist)
        {
            // Instantiate _items if it has not been initialized
            _items = _items ?? new List<T>();

            // Add the item to the internal collection
            _items.Add(item);

            // If persisting is necessary, write to database
            if (Persist)
            {
                if (CurrentContext.ContextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                    _items.WriteToDatabase(CurrentContext.ContextOptions.DatabaseConnection, SqlTransaction: CurrentContext.ContextOptions.DatabaseTransaction);
                else
                    item.WriteToDatabase(CurrentContext.ContextOptions.ConnectionStringType);
            }
            else
                Modified = true;
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(T item)
        {
            return _items?.Contains(item) ?? false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            /*
            // Validate the input array and index
            if (array == null)
                throw new ArgumentNullException(nameof(array), "The array cannot be null.");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "The starting array index cannot be negative.");
            if (array.Length - arrayIndex < (_items?.Count ?? 0))
                throw new ArgumentException("The destination array has fewer elements than the collection.");
            */
            // Copy items
            _items?.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _items?.Remove(item) ?? false;
        }
    }
}
