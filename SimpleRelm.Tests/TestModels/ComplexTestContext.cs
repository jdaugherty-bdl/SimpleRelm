using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    public class ComplexTestContext : RelmContext
    {
        public ComplexTestContext(string? connectionString) : base(connectionString, autoOpenConnection: false) { }
        public ComplexTestContext(RelmContextOptionsBuilder? options) : base(options, autoOpenConnection: false) { }

        public virtual IRelmDataSet<ComplexTestModel>? ComplexTestModels { get; set; }
        public virtual IRelmDataSet<ComplexReferenceObject>? ComplexReferenceObjects { get; set; }
    }
}
