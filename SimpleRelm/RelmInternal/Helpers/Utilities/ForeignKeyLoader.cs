using SimpleRelm.Models;
using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.RelmInternal.Helpers.Utilities
{
    internal class ForeignKeyLoader<T> where T : RelmModel, new()
    {
        private readonly ICollection<T> targetObjects;
        private readonly RelmContextOptionsBuilder contextOptionsBuilder;

        public ForeignKeyLoader(T targetObject, RelmContextOptionsBuilder relmContextOptionsBuilder) 
        {
            this.targetObjects = new[] { targetObject };
            contextOptionsBuilder = relmContextOptionsBuilder;
        }

        public ForeignKeyLoader(ICollection<T> targetObjects, )
        {
            this.targetObjects = targetObjects;
        }

        public T LoadMyForeignKey(Expression<Func<T, object>> predicate)
        {
            var ddd = AssemblyHelper.GetEntryAssembly();

            // find all objects within the current context that implements or inherits from anything impelementing the IRelmContext interface, then finds the context that contains the type T


            var objectsLoader = new ForeignObjectsLoader<T>(targetObjects, null); // _currentContext);

            objectsLoader.LoadForeignObjects(predicate.Body);

            return new T();
        }
    }
}
