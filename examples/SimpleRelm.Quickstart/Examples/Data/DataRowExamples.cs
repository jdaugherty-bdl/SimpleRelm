using SimpleRelm.Interfaces;
using SimpleRelm.Interfaces.RelmQuick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Examples.Data
{
    internal class DataRowExamples
    {
        internal void RunExamples(IRelmContext relmContext, IRelmQuickContext relmQuickContext)
        {
            // Example usage to get a DataRow using query only
            var queryOnly = @"SELECT * FROM ExampleTable 
                LIMIT 1;";

            var dataRowOnly = RelmHelper.GetDataRow(relmContext, queryOnly, ThrowException: true);
            dataRowOnly = RelmHelper.GetDataRow(relmQuickContext, queryOnly, ThrowException: true);
            dataRowOnly = relmContext.GetDataRow(queryOnly, ThrowException: true);
            dataRowOnly = relmQuickContext.GetDataRow(queryOnly, ThrowException: true);

            // Example usage to get a DataRow using query and parameters
            var parametersQuery = @"SELECT * FROM ExampleTable 
                WHERE InternalId = @guid_value
                LIMIT 1;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };

            var dataRow = RelmHelper.GetDataRow(relmContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataRow = RelmHelper.GetDataRow(relmQuickContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataRow = relmContext.GetDataRow(parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataRow = relmQuickContext.GetDataRow(parametersQuery, Parameters: exampleParameters, ThrowException: true);
        }
    }
}
