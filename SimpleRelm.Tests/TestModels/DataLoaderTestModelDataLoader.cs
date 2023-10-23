using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
using SimpleRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    internal class DataLoaderTestModelDataLoader : DefaultDataLoader<DataLoaderTestModel>
    {
        public override ICollection<DataLoaderTestModel> PullData(string selectQuery, Dictionary<string, object> findOptions)
        {
            return new List<DataLoaderTestModel>
            {
                new DataLoaderTestModel { InternalId = "ID1" },
                new DataLoaderTestModel { InternalId = "ID2" }
            };
        }
    }
}
