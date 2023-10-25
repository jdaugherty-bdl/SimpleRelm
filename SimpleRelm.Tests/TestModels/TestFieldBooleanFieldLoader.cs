using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    public class TestFieldBooleanFieldLoader : IRelmFieldLoader
    {
        private readonly string? _fieldName;
        public string? FieldName => _fieldName;

        public TestFieldBooleanFieldLoader(string fieldName)
        {
            _fieldName = fieldName;
        }

        public virtual Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData) where S : notnull
        {
            return keyData
                .Select((x, i) => new { Key = x, Value = i })
                .ToDictionary(x => x.Key, x => (object)(x.Value % 2 == 0));
        }
    }
}
