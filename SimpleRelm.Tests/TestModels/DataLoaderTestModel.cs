using SimpleRelm.Attributes;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    [RelmDataLoader(typeof(DataLoaderTestModelDataLoader))]
    public class DataLoaderTestModel : RelmModel
    {
    }
}
