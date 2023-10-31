using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
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

        public ForeignKeyLoader(T targetObject) 
        {
            this.targetObjects = new[] { targetObject };
        }

        public ForeignKeyLoader(ICollection<T> targetObjects)
        {
            this.targetObjects = targetObjects;
        }

        public T LoadMyForeignKey<R>(Expression<Func<T, R>> predicate)
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

            var currentContext = (IRelmContext)Activator.CreateInstance(relevantContext, new object[] { new MySqlConnection(), false, false });

            var objectsLoader = new ForeignObjectsLoader<T>(targetObjects, currentContext);

            objectsLoader.LoadForeignObjects(predicate.Body);

            return new T();
        }
    }
}
