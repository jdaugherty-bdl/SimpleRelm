using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    internal class ComplexTestContext : RelmContext
    {
        public ComplexTestContext() : base("name=ComplexTestContext")
        {
        }

        public IRelmDataSet<ComplexTestModel>? ComplexTestModels { get; set; }
    }
}
