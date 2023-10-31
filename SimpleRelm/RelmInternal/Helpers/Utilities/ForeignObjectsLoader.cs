using MoreLinq;
using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignObjectsLoader<T> where T : IRelmModel, new()
    {
        private ICollection<T> _items;
        private readonly IRelmContext _currentContext;

        internal ForeignObjectsLoader()
        {
        }

        internal ForeignObjectsLoader(ICollection<T> items, IRelmContext relmContext)
        {
            _items = items;
            _currentContext = relmContext;
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
        internal void LoadForeignObjects(Expression member)
        {
            if (_items == null)
                throw new InvalidOperationException("Items collection is null.");
            if (_currentContext == null)
                throw new InvalidOperationException("Current context is null.");

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
            else if (referenceType.IsGenericType)
                referenceType = referenceType.GetGenericArguments().FirstOrDefault();

            // if foreign key attribute on the current item's property, then we have principal resolution
            var principalReslolutionForeignKey = referenceProperty.Member.GetCustomAttribute<RelmForeignKey>();

            // get all RelmKeys on the main object
            var referenceKeys = GetReferenceKeys(principalReslolutionForeignKey?.LocalKeys);

            // go through all items in the current data set and collect all relmkey values
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
                //.Where(x => x.PropertyType == typeof(T) || x.PropertyType.GetGenericArguments().Contains(typeof(T)))
                .Where(x => x.PropertyType == typeof(T) || x.PropertyType.GetGenericArguments().Contains(typeof(T)))
                .ToList();

            // dependent entity has foreign key attribute/navigation property instead of principal entity
            if (principalReslolutionForeignKey == null)
            {
                // get all properties on target that have a RelmForeignKey attribute and make dictionary with LocalKeys as keys
                var targetForeignKeyDecorators = targetProperties
                    .Where(x => x.GetCustomAttribute<RelmForeignKey>() != null)
                    .ToDictionary(x => x, x => x.GetCustomAttribute<RelmForeignKey>())
                    .Segment((prev, next, i) => !prev.Value.LocalKeys.All(x => next.Value.LocalKeys.Contains(x)))
                    .ToDictionary(x => x.FirstOrDefault().Value.LocalKeys, x => x.ToDictionary(y => y.Key, y => y.Value.ForeignKeys));

                // find any navigation properties that are the same type as this data set
                var navigationProps = targetPropertiesOfTypeT
                    .Where(x => targetForeignKeyDecorators.Any(y => y.Key.Contains(x.Name)))
                    .ToList();

                // TODO: allow multiple navigation properties on target class
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

                    referenceKeys = GetReferenceKeys(targetForeignKeyDecorators
                        .SelectMany(x => x.Value.SelectMany(y => y.Value ?? new string[] { }).ToArray())
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

                navigationProperty = navigationProps.FirstOrDefault();
            }
            else
            {
                // get the principal entity's foreign key property
                foreignKeyProperties = targetProperties.Where(x => principalReslolutionForeignKey.ForeignKeys.Contains(x.Name)).ToArray();
                navigationProperty = targetPropertiesOfTypeT.FirstOrDefault(); //.Values.FirstOrDefault();
            }

            // check required variables have something in them

            if ((foreignKeyProperties?.Length ?? 0) <= 0)
                throw new MemberAccessException("Foreign key referenced by RelmForeignKey attribute could not be found.");

            if (navigationProperty == null)
                throw new MemberAccessException("Navigation property referenced by RelmForeignKey attribute could not be found.");

            if ((itemPrimaryKeys?.Count ?? 0) <= 0)
                throw new Exception("No primary keys found.");

            if ((referenceKeys?.Length ?? 0) <= 0)
                throw new Exception("No reference keys found.");

            // create a Relm expression tree to execute on the where method of the target data set, handles compound keys

            var funcType = typeof(Func<,>).MakeGenericType(referenceType, typeof(bool));
            var containsMethod = typeof(List<object>).GetMethod(nameof(List<object>.Contains));
            var whereMethod = dataSet
                .GetType()
                .GetMethods()
                .Where(m => m.Name == nameof(RelmDataSet<T>.Where))
                .First();

            var parameter = Expression.Parameter(referenceType, "x");

            BinaryExpression orExpression = null;
            foreach (var itemPrimaryKey in itemPrimaryKeys)
            {
                BinaryExpression andExpression = null;
                for (var i = 0; i < itemPrimaryKey.Count; i++)
                {
                    var memberExpression = Expression.Property(parameter, foreignKeyProperties[i].Name)
                        ?? throw new Exception("Property referenced by RelmForeignKey attribute could not be found.");

                    var equalExpression = Expression.Equal(Expression.Constant(itemPrimaryKey[i].Item2), memberExpression);

                    if (andExpression == null)
                        andExpression = equalExpression;
                    else
                        andExpression = Expression.AndAlso(andExpression, equalExpression);
                }

                if (orExpression == null)
                    orExpression = andExpression;
                else
                    orExpression = Expression.OrElse(orExpression, andExpression);
            }

            var containsLambda = Expression.Lambda(funcType, orExpression, parameter) 
                ?? throw new Exception("No contains lambda expression found.");

            var filteredDataSetContains = whereMethod.Invoke(dataSet, new object[] { containsLambda });
            var collectionItemsContains = dataSet.GetType().GetMethod(nameof(RelmDataSet<T>.Load)).Invoke(filteredDataSetContains, null);

            // use a foreach loop to convert collectionItemsContains to a dictionary where the key is the foreign key and the object is the item
            IDictionary collectionItems;
            if (isCollection)
                collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object).MakeArrayType(), typeof(List<>).MakeGenericType(referenceType)));
            else
                collectionItems = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(object).MakeArrayType(), referenceType));

            foreach (var item in (IEnumerable)dataSet)
            {
                var targetObjectForeignKeyValues = foreignKeyProperties.Select(x => x.GetValue(item)).ToArray();

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
                    ((IList)collectionItems[collectionItems.Keys.Cast<object[]>().FirstOrDefault(x => x.Select((y, i) => targetObjectForeignKeyValues[i] == y).All(y => y))]).Add(item);
                else
                    collectionItems[targetObjectForeignKeyValues] = item;
            }

            // loop through each item in _items and add the related item to the collection
            foreach (var item in _items)
            {
                var foreignKeyValues = item.GetType().GetProperties().Where(x => referenceKeys.Contains(x)).Select(x => x.GetValue(item)).ToArray();

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

        /// <summary>
        /// Searches through all properties in the current T type and identifies the property that is marked with the RelmKey attribute, overriding "InternalId" if necessary
        /// </summary>
        /// <param name="localKeyName"></param>
        /// <returns></returns>
        internal PropertyInfo GetReferenceKeys(string localKeyName)
        {
            return GetReferenceKeys(new string[] { localKeyName })?.FirstOrDefault();
        }

        internal PropertyInfo[] GetReferenceKeys(string[] localKeyNames)
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
