using SimpleRelm.Interfaces.RelmQuick;
using SimpleRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.FieldLoaders
{
    internal class IsModificationFieldLoaderQuick : IRelmQuickFieldLoader
    {
        public string FieldName { get; private set; }
        public string[] KeyFields { get; private set; }
        public IRelmQuickContext RelmContext { get; private set; }

        public IsModificationFieldLoaderQuick(IRelmQuickContext relmContext, string fieldName, string[] keyFields)
        {
            FieldName = fieldName;
            KeyFields = keyFields;
            RelmContext = relmContext ?? throw new ArgumentNullException(nameof(relmContext), "RelmContext cannot be null.");
        }

        public Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData)
        {
            var data = keyData
                .ToDictionary(x => x, x => (object)(RelmContext
                    .Get<ExampleModel>(y => y.Active == true && y.SuperceededByInternalId == x.ToString())
                    .Count > 0));

            return data;
        }
    }
}
