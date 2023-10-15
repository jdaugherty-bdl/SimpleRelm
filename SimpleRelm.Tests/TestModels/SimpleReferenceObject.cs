using SimpleRelm.Attributes;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class SimpleReferenceObject : RelmModel
    {
    }
}
