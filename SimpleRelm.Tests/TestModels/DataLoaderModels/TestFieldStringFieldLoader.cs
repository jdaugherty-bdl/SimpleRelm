using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels.DataLoaderModels
{
    internal class TestFieldStringFieldLoader : IRelmFieldLoader
    {
        public string FieldName { get; private set; }
        public string[] KeyFields { get; private set; }

        public IRelmContext RelmContext => relmContext;
        private IRelmContext relmContext;

        public TestFieldStringFieldLoader(string fieldName, string[] keyFields)
        {
            FieldName = fieldName;
            KeyFields = keyFields;

            relmContext = new ComplexTestContext();
        }

        public Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData) where S : notnull
        {
            var testContext = (ComplexTestContext)RelmContext;

            var applications = testContext
                .ComplexTestModels!
                .Where(x => x.Active == true && keyData.Any(y => y.First().ToString() == x.TestFieldString))
                .Load()
                .ToDictionary(x => keyData.First(y => y.All(z => z.ToString() == x.TestFieldString)), x => (object)x);

            return applications;
        }
    }
}
