using SimpleRelm.Interfaces;
using SimpleRelm.Quickstart.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.FieldLoaders
{
    internal class IsModificationFieldLoader : IRelmFieldLoader
    {
        public string FieldName { get; private set; }
        public string[] KeyFields { get; private set; }
        public IRelmContext RelmContext { get; private set; }

        private ExampleContext _exampleContext => RelmContext as ExampleContext;

        public IsModificationFieldLoader(IRelmContext relmContext, string fieldName, string[] keyFields)
        {
            FieldName = fieldName;
            KeyFields = keyFields;
            RelmContext = relmContext ?? throw new ArgumentNullException(nameof(relmContext), "RelmContext cannot be null.");

            if (_exampleContext == null)
                RelmContext = new ExampleContext(relmContext.ContextOptions);
        }

        public Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData)
        {
            var data = keyData
                .ToDictionary(x => x, x => (object)(_exampleContext
                    .ExampleModels
                    .Where(y => y.Active == true && y.SuperceededByInternalId == x.ToString())
                    .Load()
                    .Count > 0));

            return data;
        }
    }
}
