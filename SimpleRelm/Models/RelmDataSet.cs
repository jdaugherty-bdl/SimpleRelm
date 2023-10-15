using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Extensions;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
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

        private readonly IRelmContext _currentContext;
        private IRelmDataLoader<T> _dataLoader;

        private ICollection<T> _items;

        public RelmDataSet(IRelmContext currentContext, IRelmDataLoader<T> dataLoader)
        {
            _currentContext = currentContext ?? throw new ArgumentNullException(nameof(currentContext));

            _dataLoader = dataLoader; // new DefaultDataLoader<T>(currentContext.ContextOptions);

            Modified = false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (_items ?? Load())?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IRelmDataLoader<T> SetDataLoader(IRelmDataLoader<T> dataLoader)
        {
            _dataLoader = dataLoader;

            return _dataLoader;
        }

        internal IRelmDataLoader<T> GetDataLoader()
        {
            return _dataLoader;
        }

        public IRelmDataSet<T> Where(Expression<Func<T, bool>> predicate)
        {
            _dataLoader.AddExpression(Command.Where, predicate);

            return this;
        }

        public IRelmDataSet<T> Reference(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddExpression(Command.Reference, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Collection(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddExpression(Command.Collection, predicate.Body);

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
            _items = _dataLoader.GetLoadData();

            if (_items?.Any() ?? false)
            {
                // load all references
                if (_dataLoader.LastCommandsExecuted?.ContainsKey(Command.Reference) ?? false)
                    LoadReference();

                // load all collections
                if (_dataLoader.LastCommandsExecuted?.ContainsKey(Command.Collection) ?? false)
                    LoadCollection();
            }

            return _items;
        }

        public int Write()
        {
            return _dataLoader.WriteData();
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
            foreach (var reference in _dataLoader.LastCommandsExecuted[Command.Reference])
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
            foreach (var collection in _dataLoader.LastCommandsExecuted[Command.Collection])
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
                throw new MemberAccessException("No property found with RelmForeignKey attribute on the related entity.");
            }

            // Get the DALForeignKey attribute from the matching property
            var dalForeignKey = (((RelmForeignKey)Attribute.GetCustomAttribute(foreignKeyProperty, typeof(RelmForeignKey)))?.ForeignKey)
                ?? throw new Exception("RelmForeignKey attribute not found on related entity.");

            // Generate a Func<> type based on the generic type argument for use below
            var funcType = typeof(Func<,>).MakeGenericType(genericTypeArgument, typeof(bool));

            // get the property named by dalForeignKey from the type defined in genericTypeArgument and create a MemberExpression from it
            var parameter = Expression.Parameter(genericTypeArgument, "x");
            var memberExpression = Expression.Property(parameter, dalForeignKey)
                ?? throw new Exception("Property referenced by RelmForeignKey attribute could not be found.");

            // Instantiate a new DALContext of the same type as CurrentContext so we can load the data we need without modifying anything in our context
            var newDalContextType = _currentContext.GetType();

            // Find the DALDataSet with the same generic type as genericTypeArgument and create a new one
            var dataSetMethod = newDalContextType.GetMethod(nameof(_currentContext.GetDataSetType), new[] { typeof(Type) })
                ?? throw new InvalidOperationException("Method not found.");

            var dataSet = dataSetMethod.Invoke(_currentContext, new object[] { genericTypeArgument }) as IRelmDataSetBase
                ?? throw new InvalidOperationException($"RelmDataSet with generic type {genericTypeArgument.Name} not found.");

            // get all the foreign keys to look up
            /*
            var itemForeignKeys = _items
                .Select(x => x.GetType().GetProperty(foreignKeyAttribute.ForeignKey)?.GetValue(x))
                .Distinct()
                .ToList();
            */
            var itemForeignKeys = _items
                .Select(x => x.GetType().GetProperty(dalForeignKey)?.GetValue(x))
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
            IDictionary collectionItems;
            if (isCollection)
                collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object), typeof(List<>).MakeGenericType(genericTypeArgument)));
            else
                collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object), genericTypeArgument));

            foreach (var item in (IEnumerable)dataSet)
            {
                var targetObjectForeignKeyValue = genericTypeArgument.GetProperty(foreignKeyAttribute.ForeignKey).GetValue(item);

                if (!collectionItems.Contains(targetObjectForeignKeyValue))
                {
                    collectionItems.Add(targetObjectForeignKeyValue, default);

                    if (isCollection)
                        collectionItems[targetObjectForeignKeyValue] = Activator.CreateInstance(typeof(List<>).MakeGenericType(genericTypeArgument));
                }

                if (isCollection)
                    ((IList)collectionItems[targetObjectForeignKeyValue]).Add(item);
                else
                    collectionItems[targetObjectForeignKeyValue] = item;
            }

            // loop through each item in _items and add the related item to the collection
            foreach (var item in _items)
            {
                var foreignKeyValue = item.GetType().GetProperty(dalForeignKey).GetValue(item);
                var collectionItem = collectionItems[foreignKeyValue];
                var collectionProperty = referenceProperty.Member as PropertyInfo;

                /*
                if (isCollection)
                {
                    var collectionValue = collectionProperty.GetValue(item);
                    var methodInfo = collectionValue.GetType().GetMethod(nameof(Dictionary<object, object>.Add));

                    methodInfo.Invoke(collectionValue, new object[] { collectionItem });
                }
                else
                */
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
            _dataLoader.AddSingleExpression(Command.OrderBy, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> OrderByDescending(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddSingleExpression(Command.OrderByDescending, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Set(Expression<Func<T, T>> predicate)
        {
            _dataLoader.AddExpression(Command.Set, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> GroupBy(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddSingleExpression(Command.GroupBy, predicate.Body);

            return this;
        }

        public IRelmDataSet<T> Limit(int LimitCount)
        {
            _dataLoader.AddSingleExpression(Command.Limit, Expression.Constant(LimitCount, LimitCount.GetType()));

            return this;
        }

        public IRelmDataSet<T> DistinctBy(Expression<Func<T, object>> predicate)
        {
            _dataLoader.AddSingleExpression(Command.DistinctBy, predicate.Body);
            //_dataLoader.AddExpression(Command.Collection, predicate.Body);

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
            if (_currentContext.ContextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                rowsUpdated = _items.WriteToDatabase(_currentContext.ContextOptions.DatabaseConnection, SqlTransaction: _currentContext.ContextOptions.DatabaseTransaction);
            else
                rowsUpdated = _items.WriteToDatabase(_currentContext.ContextOptions.ConnectionStringType);

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
                    if (_dataLoader.HasUnderscoreProperty(property.Key))
                        typeof(T).GetProperty(property.Key).SetValue(newObject, property.Value);

            Add(newObject, Persist: Persist);

            return newObject;
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
                if (_currentContext.ContextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                    _items.WriteToDatabase(_currentContext.ContextOptions.DatabaseConnection, SqlTransaction: _currentContext.ContextOptions.DatabaseTransaction);
                else
                    item.WriteToDatabase(_currentContext.ContextOptions.ConnectionStringType);
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
