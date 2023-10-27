using MoreLinq;
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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Web.UI.WebControls;
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
                //foreach (var field in typeof(T).GetProperties().Where(x => x.GetCustomAttribute<RelmDataLoader>() != null))
                foreach (var field in typeof(T).GetProperties().Where(x => x.GetCustomAttribute<RelmDataLoader>()?.LoaderType?.GetInterfaces()?.Any(y => y == typeof(IRelmFieldLoader)) ?? false))
                {
                    //if (_fieldDataLoaders.ContainsKey(field.Name))
                    if (_fieldDataLoaders.HasFieldLoader(field.Name))
                        continue;

                    var fieldLoader = field.GetCustomAttribute<RelmDataLoader>().LoaderType;

                    //if (fieldLoader.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRelmFieldLoader)))
                    //if (fieldLoader.GetInterfaces().Any(x => x == typeof(IRelmFieldLoader)))
                    //{
                        //var fieldType = fieldLoader.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRelmFieldLoader)).GetGenericArguments().First();

                        //if (!_fieldDataLoaders.ContainsKey(field.Name))
                        //_fieldDataLoaders.Add(field.Name, (IRelmFieldLoader<object>)Activator.CreateInstance(fieldLoader.IsGenericTypeDefinition ? fieldLoader.MakeGenericType(fieldType) : fieldLoader)); // as IRelmFieldLoader<object>);
                        //_fieldDataLoaders.RegisterFieldLoader(field.Name, (IRelmFieldLoader)Activator.CreateInstance(fieldLoader.IsGenericTypeDefinition ? fieldLoader.MakeGenericType(fieldType) : fieldLoader, new object[] { field.Name }));
                        _fieldDataLoaders.RegisterFieldLoader(field.Name, (IRelmFieldLoader)Activator.CreateInstance(fieldLoader, new object[] { field.Name }));
                    //}
                }

                // find all fields that have the RelmKey
                var referenceKeys = GetReferenceKeys((string[])null);

                // execute all field loaders
                foreach (var fieldLoader in _fieldDataLoaders)
                {
                    // check if the field is a collection, if it is call GetFieldData the returns a list of objects, otherwise GetFieldData that return a single object




                    //var fieldData = fieldLoader.GetFieldData(_items.Select(x => x.GetType().GetProperty(referenceKey.Name).GetValue(x)).ToList());
                    var fieldData = fieldLoader.GetFieldData(_items.Select(x => x.GetType().GetProperties().Intersect(referenceKeys).Select(y => y.GetValue(x)).ToArray()).ToList());

                    foreach (var item in _items)
                    {
                        //var itemValue = item.GetType().GetProperty(referenceKeys.Name).GetValue(item);
                        var itemValues = item.GetType().GetProperties().Intersect(referenceKeys).Select(y => y.GetValue(item)).ToArray();

                        //if (fieldData.ContainsKey(itemValues))
                        if (fieldData.Keys.Any(x => x.All(y => itemValues.Contains(y))))
                        {
                            var fieldValue = fieldData.FirstOrDefault(x => x.Key.All(y => itemValues.Contains(y))).Value;

                            /*
                            //item.GetType().GetProperty(fieldLoader.FieldName).SetValue(item, fieldData[itemValues]);
                            var setField = item.GetType().GetProperty(fieldLoader.FieldName);
                            var xlist = (fieldValue as IEnumerable)?.Cast<object>()?.ToList();
                            setField.SetValue(item, xlist ?? fieldValue);
                            */
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

        /// <summary>
        /// Takes EF6-like foreign key attributes and loads the related objects into their respective data sets in the current context, with the
        /// difference that this function uses the explicitly declared [RelmKey] attribute. The foreign key may be 1) declared on the primary entity,
        /// indicating which property on the navigation entity is the foreign key, or 2) declared on the navigation entity, indicating which property
        /// is the foreign key, or 3) declared on the foreign key property itself, indicating which property is the navigation entity it is the primary
        /// key for. If no [RelmKey] is declared, will default to "InternalId".
        /// </summary>
        /// <param name="member">The property member to load references for.</param>
        /// <exception cref="InvalidOperationException">Thrown if there's an invalid operation.</exception>
        /// <exception cref="MemberAccessException">Thrown if there's an invalid member.</exception>
        /// <exception cref="Exception">Thrown if there's an exception.</exception>
        private void LoadForeignObjects(Expression member)
        {
            PropertyInfo[] foreignKeyProperties = default;
            PropertyInfo navigationProperty = default;
            List<List<Tuple<PropertyInfo, object>>> itemPrimaryKeys = default;

            var referenceProperty = member as MemberExpression
                ?? throw new InvalidOperationException("Collection must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var referenceType = referenceProperty.Type;
            var isCollection = referenceType.IsGenericType && referenceType.GetGenericTypeDefinition() == typeof(ICollection<>);

            // The type of class being referenced by the collection command
            if (isCollection)
            {
                referenceType = referenceType.GetGenericArguments()[0];

                // Check if the referenceType is compatible with ICollection<>
                if (!typeof(ICollection<>).MakeGenericType(referenceType).IsAssignableFrom(referenceProperty.Type))
                    throw new InvalidOperationException($"Reference property type must be compatible with ICollection<{referenceType}>.");
            }

            // if foreign key attribute on the current item's property, then we have principal resolution
            var principalReslolutionForeignKey = referenceProperty.Member.GetCustomAttribute<RelmForeignKey>();

            // get all RelmKeys on the main object
            var referenceKeys = GetReferenceKeys(principalReslolutionForeignKey?.LocalKeys);

            // go through all items in the current data set and collect all relmkey values
            /*
            itemPrimaryKeys = _items
                .Select(x => x.GetType().GetProperties().Intersect(referenceKeys)?.Select(y => y.GetValue(x)).ToArray())
                .Distinct()
                .ToList();
            */
            /*
            itemPrimaryKeys = typeof(T)
                    .GetProperties()
                    .Intersect(referenceKeys)
                    .Select(x => new Tuple<PropertyInfo, List<object>>(x,  _items.Select(y => y.GetType().GetProperty(x.Name, x.PropertyType).GetValue(y)).ToList()))
                    .ToList();
            */
            itemPrimaryKeys = _items
                .Select(x => x
                    .GetType()
                    .GetProperties()
                    .Intersect(referenceKeys)
                    .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                    .ToList())
                .ToList();

            //if ((itemPrimaryKeys?.Count ?? 0) <= 0)
            if (itemPrimaryKeys == null)
                throw new Exception("No primary keys found.");

            // Instantiate a new DALContext of the same type as CurrentContext so we can load the data we need without modifying anything in our context
            var newDalContextType = _currentContext.GetType();

            // Find the DALDataSet with the same generic type as referenceType and create a new one
            var dataSetMethod = newDalContextType.GetMethod(nameof(_currentContext.GetDataSetType), new[] { typeof(Type) })
                ?? throw new InvalidOperationException("Method not found.");

            var dataSet = dataSetMethod.Invoke(_currentContext, new object[] { referenceType }) //as IRelmDataSetBase
                ?? throw new InvalidOperationException($"RelmDataSet with generic type {referenceType.Name} not found.");

            var targetProperties = dataSet.GetType().GetGenericArguments().FirstOrDefault().GetProperties();

            // make a list of all targetProperties that are of type T
            var targetPropertiesOfTypeT = targetProperties
                .Where(x => x.PropertyType == typeof(T) || x.PropertyType.GetGenericArguments().Contains(typeof(T)))
                .ToList();

            if (principalReslolutionForeignKey == null)
            {
                // dependent property has foreign key attribute


                // get all properties on target that have a RelmForeignKey attribute, segment by LocalKeys, make dictionary with LocalKeys as keys
                var targetForeignKeyDecorators = targetProperties
                    .Where(x => x.GetCustomAttribute<RelmForeignKey>() != null)
                    .ToDictionary(x => x, x => x.GetCustomAttribute<RelmForeignKey>())
                    .Segment((prev, next, i) => !prev.Value.LocalKeys.All(x => next.Value.LocalKeys.Contains(x)))
                    .ToDictionary(x => x.FirstOrDefault().Value.LocalKeys, x => x.ToDictionary(y => y.Key, y => y.Value.ForeignKeys));

                // get intersection between each list in that list and targetPropertiesOfTypeT
                var navigationProps = targetPropertiesOfTypeT
                    .Where(x => targetForeignKeyDecorators.Any(y => y.Key.Contains(x.Name)))
                    .ToList();

                if (navigationProps.Count > 1)
                    throw new Exception("Multiple navigation properties found.");

                if (navigationProps.Count == 0)
                {
                    // we're using navigation properties
                    navigationProps = targetPropertiesOfTypeT
                        .Where(x => targetForeignKeyDecorators.Any(y => y.Value.ContainsKey(x)))
                        .ToList();

                    foreignKeyProperties = targetForeignKeyDecorators
                        .Select(x => targetProperties.Where(y => x.Key.Contains(y.Name)).ToArray())
                        .FirstOrDefault();

                    referenceKeys = GetReferenceKeys(targetForeignKeyDecorators
                        .SelectMany(x => x.Value.Select(y => y.Value).ToArray())
                        .FirstOrDefault());

                    itemPrimaryKeys = _items
                        .Select(x => x
                            .GetType()
                            .GetProperties()
                            .Intersect(referenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                            .ToList())
                        .ToList();
                }
                else
                {
                    // we're using foreign key properties
                    foreignKeyProperties = targetForeignKeyDecorators
                        .Select(x => x.Value.Keys.ToArray())
                        .FirstOrDefault();

                    var fff = targetForeignKeyDecorators
                        .SelectMany(x => x.Value.Select(y => y.Value).ToArray())
                        .ToArray();

                    //if (fff.All(x => x == null))
                    {
                        referenceKeys = GetReferenceKeys(targetForeignKeyDecorators
                            .SelectMany(x => x.Value.SelectMany(y => y.Value).ToArray())
                            .ToArray());

                        itemPrimaryKeys = _items
                            .Select(x => x
                                .GetType()
                                .GetProperties()
                                .Intersect(referenceKeys)
                                .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                                .ToList())
                            .ToList();
                    }
                }

                navigationProperty = navigationProps.FirstOrDefault();

                /*
                var navProps = targetForeignKeyDecorators
                    .Where(x => x.Key.Intersect(navigationProps.Select(y => y.Name)).Any())
                    .ToDictionary(x => x.Key, x => x.Value);

                var forProps = targetForeignKeyDecorators
                    .ExceptBy(navProps, x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Value);

                foreach (var unfilteredForeignKey in targetForeignKeyDecorators)
                {
                    // separate unfiltered into nav props and foreign key props
                    var navProps = unfilteredForeignKey
                    var foreignKeyProps = unfilteredForeignKey.Value.LocalKeys
                        .Select(x => targetProperties.FirstOrDefault(y => y.Name == x))
                        .Where(x => x != null)
                        .ToList();
                }
                */
                /*
                // if the intersection is not empty, then we're using foreign key processing
                if (navigationProps.Count > 0)
                {
                    var foreignKeysIndicated = unfilteredForeignKeys
                        .ToDictionary(x => x.Key, x => typeof(T)
                            .GetProperties()
                            .Where(y => x.Value.Any(z => z.ForeignKeys?.Contains(y.Name) ?? false))
                            .ToList())
                        .Where(x => x.Value.Count > 0)
                        .ToDictionary(x => x.Key, x => x.Value);

                    var localKeysIndicated = unfilteredForeignKeys
                        .ToDictionary(x => x.Key, x => targetPropertiesOfTypeT
                            .Where(y => x.Value.Any(z => z.LocalKeys?.Contains(y.Name) ?? false))
                            .ToList())
                        .Where(x => x.Value.Count > 0)
                        .ToDictionary(x => x.Key, x => x.Value);

                    navigationProps = targetPropertiesOfTypeT
                        .Where(x => localKeysIndicated.SelectMany(y => y.Value).Any(y => y.Name == x.Name))
                        .ToList();

                    navigationProperty = navigationProps.FirstOrDefault();
                    foreignKeyProperties = foreignKeysIndicated.SelectMany(x => x.Value).ToArray();

                    if (foreignKeysIndicated.Count > 0)
                    {
                        referenceKeys = GetReferenceKeys(foreignKeysIndicated.Values.SelectMany(x => x.Select(y => y.Name)).ToArray());
                    }
                }
                */
                /*
                // if the intersection is not empty, then we're using navigation property processing

                // check if navigation or foreign key
                var foreignKeyProps = targetProperties
                    .Where(x => x.GetCustomAttribute<RelmForeignKey>() != null) // && x.PropertyType == typeof(T))
                    .ToArray();
                */
                /*

                //var navigationProp = foreignKeyProperties.FirstOrDefault(x => x.PropertyType == typeof(T));

                // is foreign key
                if (navigationProp == null)
                {
                    var navigationProps = targetPropertiesOfTypeT
                        .Where(x => foreignKeyProperties x.Name)
                }
                */
                /*
                if (!foreignKeyProperties.Contains(navigationProperty))
                {
                    foreignKeyProperties = targetProperties
                        .Where(x => x.GetCustomAttribute<RelmForeignKey>()?.LocalKeys?.Intersect(targetPropertiesOfTypeT.Select(y => y.Name)).Any() ?? false)
                        .ToArray();

                    navigationProperty = targetPropertiesOfTypeT
                        .FirstOrDefault(y => foreignKeyProperties.Any(x => x.GetCustomAttribute<RelmForeignKey>().LocalKeys.Contains(y.Name)));
                }

                if (foreignKeyProperties.Any(x => x.GetCustomAttribute<RelmForeignKey>()?.ForeignKeys != null))
                {
                    referenceKeys = GetReferenceKeys(principalReslolutionForeignKey?.LocalKeys);

                    itemPrimaryKeys = _items
                        .Select(x => x
                            .GetType()
                            .GetProperties()
                            .Intersect(referenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                            .ToList())
                        .ToList();

                    if (itemPrimaryKeys == null)
                        throw new Exception("No primary keys found.");
                }
                */
                /*
                var foreignKeyValues = foreignKeyProps
                    .ToDictionary(x => x, x => x.GetCustomAttribute<RelmForeignKey>().ForeignKeys);

                var localKeyValues = foreignKeyProps
                    .Where(x => (x.GetCustomAttribute<RelmForeignKey>().LocalKeys?.Length ?? 0) > 0)
                    .ToDictionary(x => x, x => x.GetCustomAttribute<RelmForeignKey>().LocalKeys);

                if (localKeyValues.Count > 0)
                {
                    referenceKeys = GetReferenceKeys(principalReslolutionForeignKey?.LocalKeys);

                    itemPrimaryKeys = _items
                        .Select(x => x
                            .GetType()
                            .GetProperties()
                            .Intersect(referenceKeys)
                            .Select(y => new Tuple<PropertyInfo, object>(y, y.GetValue(x)))
                            .ToList())
                        .ToList();

                    if (itemPrimaryKeys == null)
                        throw new Exception("No primary keys found.");
                }

                // navigation property on the dependent property
                var foreignKeyInfo = targetPropertiesOfTypeT
                    .Intersect(foreignKeyValues.Keys)
                    .Select(x => new
                    {
                        ForeignKeys = targetProperties.Where(y => foreignKeyValues[x].Contains(y.Name)).ToArray(), // .FirstOrDefault(y => y.Key.Name == foreignKeyValues[x])
                        NavigationProperty = x,
                    })
                    .FirstOrDefault()

                    ??

                    // foreign key property on the dependent property
                    foreignKeyProps
                    .Select(x => new
                    {
                        ForeignKeys = new PropertyInfo[] { x },
                        NavigationProperty = targetPropertiesOfTypeT
                            //.FirstOrDefault(y => y.Key.Name == foreignKeyValues[x])
                            .FirstOrDefault(y => foreignKeyValues[x].Contains(y.Name))
                            //.Value,
                    })
                    .FirstOrDefault()

                    ??

                    throw new NullReferenceException("No foreign key info found.");

                foreignKeyProperties = foreignKeyInfo.ForeignKeys;
                navigationProperty = foreignKeyInfo.NavigationProperty;
                */
            }
            else
            {
                // get the primary entity's foreign key property
                foreignKeyProperties = targetProperties.Where(x => principalReslolutionForeignKey.ForeignKeys.Contains(x.Name)).ToArray();
                navigationProperty = targetPropertiesOfTypeT.FirstOrDefault(); //.Values.FirstOrDefault();
            }

            // if foreignKeyProperty is null, throw an exception
            if (foreignKeyProperties == null)
                throw new MemberAccessException("Property referenced by RelmForeignKey attribute could not be found.");

            // if navigationProperty is null, throw an exception
            if (navigationProperty == null)
                throw new MemberAccessException("Property referenced by RelmForeignKey attribute could not be found.");

            if (itemPrimaryKeys == null)
                throw new Exception("No primary keys found.");

            // Generate a Func<> type based on the generic type argument for use below
            var funcType = typeof(Func<,>).MakeGenericType(referenceType, typeof(bool));

            // look up the Contains method on the itemForeignKeys type, then make a generic method with the memberExpression type
            /*
            var containsMethod = itemPrimaryKeys
                .GetType()
                .GetMethod(nameof(List<object>.Contains));
            */
            var containsMethod = typeof(List<object>).GetMethod(nameof(List<object>.Contains));

            // Get the "Where" method from the data set
            var whereMethod = dataSet
                .GetType()
                .GetMethods()
                .Where(m => m.Name == nameof(RelmDataSet<T>.Where))
                .First();

            // get the property named by dalForeignKey from the type defined in genericTypeArgument and create a MemberExpression from it
            var parameter = Expression.Parameter(referenceType, "x");

            /*
            BinaryExpression andExpression;
            foreach (var itemPrimaryKey in itemPrimaryKeys)
            {
                for (var i = 0; i < itemPrimaryKey.Count; i++)
                {
                    var memberExpression = Expression.Property(parameter, foreignKeyProperties[i].Name)
                        ?? throw new Exception("Property referenced by RelmForeignKey attribute could not be found.");
                    var containsExpression = Expression.Call(Expression.Constant(itemPrimaryKeys[i].Values), containsMethod, memberExpression);

                    andExpressions.Add(Expression.AndAlso(containsExpression, Expression.Lambda(funcType, containsExpression, parameter)));
                }

                var orExpression = andExpressions.Aggregate(Expression.OrElse);
            }
            */
            /*
            List<LambdaExpression> containsLambda = new List<LambdaExpression>();
            foreach (var itemPrimaryKey in itemPrimaryKeys)
            {
                //var containsExpressions = new List<MethodCallExpression>();
                var containsExpressions = new List<Expression>();
                for (var i = 0; i < itemPrimaryKey.Count; i++)
                {
                    var memberExpression = Expression.Property(parameter, foreignKeyProperties[i].Name)
                            ?? throw new Exception("Property referenced by RelmForeignKey attribute could not be found.");
                    //containsExpressions.Add(Expression.Call(Expression.Constant(itemPrimaryKey[i].Item2), containsMethod, memberExpression));
                    containsExpressions.Add(Expression.Equal(Expression.Constant(itemPrimaryKey[i].Item2), memberExpression));
                }

                BinaryExpression andExpression = null;
                if (containsExpressions.Count > 1)
                {
                    for (var i = 1; i < containsExpressions.Count; i++)
                    {
                        if (i == 1)
                            andExpression = Expression.AndAlso(containsExpressions[i - 1], containsExpressions[i]);
                        else
                            andExpression = Expression.AndAlso(andExpression, containsExpressions[i]);
                    }
                }
            }

            // Apply the "Where" and "Load" methods
            //var filteredDataSetContains = whereMethod.Invoke(dataSet, new object[] { Expression.Lambda(funcType, containsExpression, parameter) });
            if (andExpression == null)
                containsLambda.Add(Expression.Lambda(funcType, containsExpressions.FirstOrDefault(), parameter));
            else
                containsLambda.Add(Expression.Lambda(funcType, andExpression, parameter));
            */
            //var orParameters = new List<List<BinaryExpression>>();
            BinaryExpression orExpression = null;
            foreach (var itemPrimaryKey in itemPrimaryKeys)
            {
                //var andParameters = new List<BinaryExpression>();
                BinaryExpression andExpression = null;
                for (var i = 0; i < itemPrimaryKey.Count; i ++)
                {
                    var memberExpression = Expression.Property(parameter, foreignKeyProperties[i].Name)
                        ?? throw new Exception("Property referenced by RelmForeignKey attribute could not be found.");

                    //andParameters.Add(Expression.Equal(Expression.Constant(itemPrimaryKey[i].Item2), memberExpression));
                    var equalExpression = Expression.Equal(Expression.Constant(itemPrimaryKey[i].Item2), memberExpression);

                    if (andExpression == null)
                        andExpression = equalExpression;
                    else
                        andExpression = Expression.AndAlso(andExpression, equalExpression);
                }

                /*
                //orParameters.Add(andParameters);
                var andExpression = andParameters.FirstOrDefault();
                if (andParameters.Count > 1)
                {
                    for (var i = 1; i < andParameters.Count; i++)
                    {
                        if (i == 1)
                            andExpression = Expression.AndAlso(andParameters[i - 1], andParameters[i]);
                        else
                            andExpression = Expression.AndAlso(andExpression, andParameters[i]);
                    }
                }

                if (orParameters.Count > 1)
                {
                    if (orExpression == null)
                        orExpression = andExpression;
                    else
                        orExpression = Expression.OrElse(orExpression, andExpression);
                }
                else
                    orExpression = andExpression;
                */
                if (orExpression == null)
                    orExpression = andExpression;
                else
                    orExpression = Expression.OrElse(orExpression, andExpression);
            }

            /*
            foreach (var andParameters in orParameters)
            {
                BinaryExpression andExpression = andParameters.FirstOrDefault();
                if (andParameters.Count > 1)
                {
                    for (var i = 1; i < andParameters.Count; i++)
                    {
                        if (i == 1)
                            andExpression = Expression.AndAlso(andParameters[i - 1], andParameters[i]);
                        else
                            andExpression = Expression.AndAlso(andExpression, andParameters[i]);
                    }
                }

                if (orParameters.Count > 1)
                {
                    if (orExpression == null)
                        orExpression = andExpression;
                    else
                        orExpression = Expression.OrElse(orExpression, andExpression);
                }
                else
                    orExpression = andExpression;
            }
            */
            /*
            LambdaExpression containsLambda = null;
            if (orExpression == null)
                containsLambda.Add(Expression.Lambda(funcType, containsExpressions.FirstOrDefault(), parameter));
            else
                containsLambda.Add(Expression.Lambda(funcType, orExpression, parameter));
            */
            var containsLambda = Expression.Lambda(funcType, orExpression, parameter);

            if (containsLambda == null)
                throw new Exception("No contains lambda expression found.");

            var filteredDataSetContains = whereMethod.Invoke(dataSet, new object[] { containsLambda });
            var collectionItemsContains = dataSet.GetType().GetMethod(nameof(Load)).Invoke(filteredDataSetContains, null);

            // use a foreach loop to convert collectionItemsContains to a dictionary where the key is the foreign key and the object is the item
            IDictionary collectionItems;
            if (isCollection)
                collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object).MakeArrayType(), typeof(List<>).MakeGenericType(referenceType)));
            else
                collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object).MakeArrayType(), referenceType));

            foreach (var item in (IEnumerable)dataSet)
            {
                var targetObjectForeignKeyValues = foreignKeyProperties.Select(x => x.GetValue(item)).ToArray();

                //if (!collectionItems.Contains(targetObjectForeignKeyValues))
                if (collectionItems.Keys.Cast<object[]>().FirstOrDefault(x => x.Select((y, i) => targetObjectForeignKeyValues[i] == y).All(y => y)) == null)
                {
                    collectionItems.Add(targetObjectForeignKeyValues, default);

                    if (isCollection)
                        collectionItems[targetObjectForeignKeyValues] = Activator.CreateInstance(typeof(List<>).MakeGenericType(referenceType));
                }
                else if (!isCollection)
                {
                    // if the collectionItems already contains the key and it's not a collection, throw an exception
                    throw new Exception("Collection already contains an item with the same foreign key.");
                }

                if (isCollection)
                    //((IList)collectionItems[targetObjectForeignKeyValues]).Add(item);
                    ((IList)collectionItems[collectionItems.Keys.Cast<object[]>().FirstOrDefault(x => x.Select((y, i) => targetObjectForeignKeyValues[i] == y).All(y => y))]).Add(item);
                else
                    collectionItems[targetObjectForeignKeyValues] = item;
            }

            // loop through each item in _items and add the related item to the collection
            foreach (var item in _items)
            {
                var foreignKeyValues = item.GetType().GetProperties().Where(x => referenceKeys.Contains(x)).Select(x => x.GetValue(item)).ToArray();

                /*
                var collectionItem = collectionItems[foreignKeyValues];
                
                var collectionProperty = referenceProperty.Member as PropertyInfo;
                collectionProperty.SetValue(item, collectionItem);
                */
                foreach (DictionaryEntry entry in collectionItems)
                {
                    // note: all keys should be in the same order as the foreign key values here
                    if (((object[])entry.Key).Select((x, i) => foreignKeyValues[i] == x).All(x => x))
                    {
                        (referenceProperty.Member as PropertyInfo).SetValue(item, entry.Value);

                        break;
                    }
                }
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

        /// <summary>
        /// Searches through all properties in the current T type and identifies the property that is marked with the RelmKey attribute, overriding "InternalId" if necessary
        /// </summary>
        /// <param name="localKeyNames"></param>
        /// <returns></returns>
        private PropertyInfo GetReferenceKeys(string localKeyNames)
        {
            return GetReferenceKeys(new string[] { localKeyNames })?.FirstOrDefault();
        }

        private PropertyInfo[] GetReferenceKeys(string[] localKeyNames)
        {
            PropertyInfo[] referenceKeys;
            //if (!string.IsNullOrWhiteSpace(localKeyNames))
            if ((localKeyNames?.Length ?? 0) > 0)
                referenceKeys = typeof(T).GetProperties().Where(x => localKeyNames.Contains(x.Name)).ToArray();
            else
            {
                var referenceRelmKeys = typeof(T).GetProperties().Where(x => x.GetCustomAttribute<RelmKey>() != null).ToArray();

                referenceKeys = referenceRelmKeys; //.FirstOrDefault();
                /*
                if (referenceRelmKeys.Count > 1)
                    referenceKeys = referenceRelmKeys.FirstOrDefault(x => x.Name != nameof(RelmModel.InternalId));
                */
            }

            return referenceKeys;
        }
    }
}
