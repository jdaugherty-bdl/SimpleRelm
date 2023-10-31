using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Options;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.RelmInternal.Helpers.DataTransfer
{
    internal class DataLoaderHelper<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T> targetObjects;

        public DataLoaderHelper(T targetObject)
        {
            this.targetObjects = new[] { targetObject };
        }

        public DataLoaderHelper(ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
        }

        internal ICollection<T> LoadField<R>(Expression<Func<T, R>> predicate)
        {
            var referenceProperty = predicate.Body as MemberExpression
                ?? throw new InvalidOperationException("Collection or property must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var dataLoaderAttribute = referenceProperty.Member.GetCustomAttribute<RelmDataLoader>()
                ?? throw new MemberAccessException("The property or collection you are trying to load does not have a RelmDataLoader attribute.");

            var fieldLoader = (IRelmFieldLoader)Activator.CreateInstance(dataLoaderAttribute.LoaderType, new object[] { referenceProperty.Member.Name, dataLoaderAttribute.KeyFields });

            new FieldLoaderHelper<T>(targetObjects).LoadData(fieldLoader);

            return targetObjects;
        }
    }
}
