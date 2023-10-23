using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.Options;
using SimpleRelm.Tests.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    public class ComplexTestContext : RelmContext, IRelmContext_TESTING
    {
        public ComplexTestContext(string? connectionString) : base(connectionString, autoOpenConnection: false) { }
        public ComplexTestContext(RelmContextOptionsBuilder? options) : base(options, autoOpenConnection: false) { }

        public virtual IRelmDataSet<ComplexTestModel>? ComplexTestModels { get; set; }
        public virtual IRelmDataSet<ComplexReferenceObject>? ComplexReferenceObjects { get; set; }
        public virtual IRelmDataSet<ComplexReferenceObject_NavigationProperty>? ComplexReferenceObject_NavigationProperties { get; set; }
        public virtual IRelmDataSet<ComplexReferenceObject_PrincipalEntity>? ComplexReferenceObject_PrincipalEntities { get; set; }
        public virtual IRelmDataSet<SimpleReferenceObject>? SimpleReferenceObjects { get; set; }
        public virtual IRelmDataSet<DataLoaderTestModel>? DataLoaderTestModels { get; set; }

        void IRelmContext_TESTING.SetDataSet<T>(IRelmDataSet<T> dataSet)
        {
            base.SetDataSet(dataSet);
        }
    }
}
