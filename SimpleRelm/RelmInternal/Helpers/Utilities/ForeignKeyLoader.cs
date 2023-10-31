using MySql.Data.MySqlClient;
using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignKeyLoader<T> where T : IRelmModel, new()
    {
        private readonly ICollection<T> targetObjects;
        private readonly RelmContextOptionsBuilder contextOptions;

        public ForeignKeyLoader(T targetObject, RelmContextOptionsBuilder contextOptions) 
        {
            this.targetObjects = new[] { targetObject };
            this.contextOptions = contextOptions;
        }

        public ForeignKeyLoader(ICollection<T> targetObjects, RelmContextOptionsBuilder contextOptions)
        {
            this.targetObjects = targetObjects;
            this.contextOptions = contextOptions;
        }

        public ICollection<T> LoadMyForeignKey<R>(Expression<Func<T, R>> predicate)
        {
            // get all types in the context assembly and look for one that inherits from RelmContext
            var relevantContext = Assembly
                .GetAssembly(typeof(T))
                .GetTypes()
                .Where(x => x.BaseType == typeof(RelmContext))
                .FirstOrDefault(x => x
                    .GetProperties()
                    .Where(y => y.PropertyType == typeof(IRelmDataSet<T>))
                    .Any());

            var relevantDataSet = relevantContext.GetProperties().FirstOrDefault(x => x.PropertyType == typeof(IRelmDataSet<T>));

            var relevantProperty = relevantDataSet.PropertyType.GetGenericArguments().FirstOrDefault().GetProperties().FirstOrDefault(x => x.PropertyType == typeof(T))
                ?? relevantDataSet.PropertyType.GetGenericArguments().FirstOrDefault().GetProperties().FirstOrDefault(x => x.PropertyType.GenericTypeArguments.Any(y => y == typeof(T)));

            var currentContext = (IRelmContext)Activator.CreateInstance(relevantContext, new object[] { contextOptions });


            var member = predicate.Body;
            var referenceProperty = member as MemberExpression
                ?? throw new InvalidOperationException("Collection must be represented by a lambda expression in the form of 'x => x.PropertyName'.");

            var referenceType = referenceProperty.Type;
            var dataLoaderAttribute = referenceProperty.Member.GetCustomAttribute<RelmDataLoader>();

            if (dataLoaderAttribute != null)
            {
                var currentDataSet = relevantContext.GetProperty(relevantDataSet.Name, typeof(IRelmDataSet<T>));
                var ddd = (IRelmDataSet<T>)relevantDataSet.GetValue(currentContext);

                var mmm = ddd.Load();
            }
            else
            {
                var objectsLoader = new ForeignObjectsLoader<T>(targetObjects, currentContext);

                objectsLoader.LoadForeignObjects(predicate.Body);
            }

            return new T[] { };
        }
    }
}
