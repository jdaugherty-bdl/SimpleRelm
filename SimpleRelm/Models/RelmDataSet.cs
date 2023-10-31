using MoreLinq;
using SimpleRelm.Attributes;
using SimpleRelm.Extensions;
using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Extensions;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
using SimpleRelm.RelmInternal.Helpers.Operations;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Web.UI.WebControls;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.Models
{
    public class RelmDataSet<T> : ICollection<T>, IRelmDataSet<T> where T : IRelmModel, new()
    {
        public bool Modified { get; set; }

        public int Count => _items?.Count ?? 0;
        public bool IsReadOnly => _items?.IsReadOnly ?? true;

        private readonly IRelmContext _currentContext;
        private IRelmDataLoader<T> _dataLoader;
        //private Dictionary<string, IRelmFieldLoader<object>> _fieldDataLoaders;
        private FieldLoaderRegistry _fieldDataLoaders;

        private ICollection<T> _items;

        public RelmDataSet(IRelmContext currentContext, IRelmDataLoader<T> dataLoader)
        {
            _currentContext = currentContext ?? throw new ArgumentNullException(nameof(currentContext));
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));

            //_fieldDataLoaders = new Dictionary<string, IRelmFieldLoader<object>>();
            _fieldDataLoaders = new FieldLoaderRegistry();

            Modified = false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            // get cached items if not null, otherwise load new items list if not null, otherwise return empty collection
            return (_items ?? Load() ?? Enumerable.Empty<T>())?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IRelmFieldLoader SetFieldLoader(string fieldName, IRelmFieldLoader dataLoader)
        {
            if (!typeof(T).GetProperties().Any(x => x.Name == fieldName))
                throw new ArgumentException($"The field {fieldName} does not exist on the model {typeof(T).Name}");

            return _fieldDataLoaders.RegisterFieldLoader(fieldName, dataLoader);
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

        /*
        public int Count()
        {

        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            _dataLoader.AddExpression(Command.Count, predicate.Body);

            _items = Load();

            return _items.Count();
        }
        */

        public ICollection<T> Load()
        {
            _items = _dataLoader.GetLoadData();

            if (_items?.Any() ?? false)
            {
                // find all fields marked with a RelmFieldLoader attribute that have a type derived from IRelmFieldLoader<> and add them to the list of field loaders as long as they are not already there
                foreach (var field in typeof(T).GetProperties().Where(x => x.GetCustomAttribute<RelmDataLoader>()?.LoaderType?.GetInterfaces()?.Any(y => y == typeof(IRelmFieldLoader)) ?? false))
                {
                    if (_fieldDataLoaders.HasFieldLoader(field.Name))
                        continue;

                    _fieldDataLoaders.RegisterFieldLoader(field.Name, (IRelmFieldLoader)Activator.CreateInstance(field.GetCustomAttribute<RelmDataLoader>().LoaderType, new object[] { field.Name, field.GetCustomAttribute<RelmDataLoader>().KeyFields }));
                }

                // execute all field loaders
                foreach (var fieldLoader in _fieldDataLoaders)
                {
                    var referenceKeys = new ForeignObjectsLoader<T>().GetReferenceKeys(fieldLoader.KeyFields);

                    // get relevant data for items in the current data set all at once to reduce number of database calls
                    var fieldData = fieldLoader.GetFieldData(_items.Select(x => x.GetType().GetProperties().Intersect(referenceKeys).Select(y => y.GetValue(x)).ToArray()).ToList());

                    // set the relevant field value on all items in the current data set
                    foreach (var item in _items)
                    {
                        var itemValues = item.GetType().GetProperties().Intersect(referenceKeys).Select(y => y.GetValue(item)).ToArray();

                        if (fieldData.Keys.Any(x => x.All(y => itemValues.Contains(y))))
                        {
                            var fieldValue = fieldData.FirstOrDefault(x => x.Key.All(y => itemValues.Contains(y))).Value;

                            var setField = item.GetType().GetProperty(fieldLoader.FieldName);
                            if (setField != null && setField.PropertyType.IsGenericType && setField.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                            {
                                var genericType = setField.PropertyType.GetGenericArguments()[0];

                                if (fieldValue is IEnumerable)
                                {
                                    var xlist = (fieldValue as IEnumerable)?.Cast<object>()?.ToList();
                                    var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(genericType);
                                    var toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList)).MakeGenericMethod(genericType);
                                    var castedList = toListMethod.Invoke(null, new object[] { castMethod.Invoke(null, new object[] { xlist }) });

                                    setField.SetValue(item, castedList);
                                }
                                else
                                {
                                    setField.SetValue(item, fieldValue);
                                }
                            }
                            else
                            {
                                // Handle cases where setField is not a List<T> or is null
                                setField?.SetValue(item, fieldValue);
                            }
                        }
                    }
                }

                // load all references
                if (_dataLoader.LastCommandsExecuted?.ContainsKey(Command.Reference) ?? false)
                    LoadReference();
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
            var objectsLoader = new ForeignObjectsLoader<T>(_items, _currentContext);

            foreach (var reference in _dataLoader.LastCommandsExecuted[Command.Reference])
            {
                objectsLoader.LoadForeignObjects(reference);
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

        public int Save(T Item)
        {
            // check if the item is already in the list, and if so, replace it, otherwise, add it
            if (_items?.Any(x => x.InternalId == Item.InternalId) ?? false)
            {
                _items = _items.Select(x => x.InternalId == Item.InternalId ? Item : x).ToList();

                return Save();
            }
            else
                return Add(Item, Persist: true);
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

        void ICollection<T>.Add(T item)
        {
            Add(item, true);
        }

        public int Add(T item)
        {
            return Add(item, true);
        }

        public int Add(T item, bool Persist)
        {
            // Instantiate _items if it has not been initialized
            _items = _items ?? new List<T>();

            // Add the item to the internal collection
            _items.Add(item);

            // If persisting is necessary, write to database
            if (Persist)
            {
                if (_currentContext.ContextOptions.OptionsBuilderType == Options.RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                    return _items.WriteToDatabase(_currentContext.ContextOptions.DatabaseConnection, SqlTransaction: _currentContext.ContextOptions.DatabaseTransaction);
                else
                    return item.WriteToDatabase(_currentContext.ContextOptions.ConnectionStringType);
            }
            else
                Modified = true;

            return 1;
        }

        public int Add(ICollection<T> items)
        {
            return Add(items, true);
        }

        public int Add(ICollection<T> items, bool Persist)
        {
            var itemCounter = 0;
            foreach (T item in items)
            {
                Add(item, false);
                itemCounter++;
            }

            if (Persist)
                return Save();

            return itemCounter;
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
