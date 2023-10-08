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
    internal class ComplexTestContext : RelmContext, IRelmContext
    {
        public ComplexTestContext(string? connectionString) : base(connectionString) { }
        public ComplexTestContext(RelmContextOptionsBuilder? options) : base(options) { }

        public virtual IRelmDataSet<ComplexTestModel>? ComplexTestModels { get; set; }
    }
}
