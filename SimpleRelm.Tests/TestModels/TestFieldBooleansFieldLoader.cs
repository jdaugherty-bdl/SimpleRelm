using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    public class TestFieldBooleansFieldLoader : IRelmFieldLoader
    {
        private readonly string? _fieldName;
        public string? FieldName => _fieldName;

        public TestFieldBooleansFieldLoader(string fieldName)
        {
            _fieldName = fieldName;
        }

        public virtual Dictionary<S, object> GetFieldData<S>(ICollection<S> keyData) where S : notnull
        {
            return keyData
                .Select((x, i) => new { Key = x, Value = i })
                .ToDictionary(x => x.Key, x => (object)Enumerable.Range(0, 3).Select(y => y % 2 == 0));
        }
    }
}
