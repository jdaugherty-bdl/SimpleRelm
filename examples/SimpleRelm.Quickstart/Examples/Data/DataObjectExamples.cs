using SimpleRelm.Extensions;
using SimpleRelm.Interfaces;
using SimpleRelm.Interfaces.RelmQuick;
using SimpleRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Examples.Data
{
    internal class DataObjectExamples
    {
        internal void RunExamples(IRelmContext relmContext, IRelmQuickContext relmQuickContext)
        {
            // Example usage to get a DataObject using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()}
                LIMIT 1;";

            var dataObjectOnly = RelmHelper.GetDataObject<ExampleModel>(relmContext, queryOnly, ThrowException: true);
            dataObjectOnly = RelmHelper.GetDataObject<ExampleModel>(relmQuickContext, queryOnly, ThrowException: true);
            dataObjectOnly = relmContext.GetDataObject<ExampleModel>(queryOnly, ThrowException: true);
            dataObjectOnly = relmQuickContext.GetDataObject<ExampleModel>(queryOnly, ThrowException: true);

            // Example usage to get a DataObject using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE InternalId = @guid_value
                LIMIT 1;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };

            var dataObject = RelmHelper.GetDataObject<ExampleModel>(relmContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataObject = RelmHelper.GetDataObject<ExampleModel>(relmQuickContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataObject = relmContext.GetDataObject<ExampleModel>(parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataObject = relmQuickContext.GetDataObject<ExampleModel>(parametersQuery, Parameters: exampleParameters, ThrowException: true);

            // Example usage to get multiple DataObjects using query only
            var multipleQueryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()}
                LIMIT 10;";
            var dataObjectsOnly = RelmHelper.GetDataObjects<ExampleModel>(relmContext, multipleQueryOnly, ThrowException: true);
            dataObjectsOnly = RelmHelper.GetDataObjects<ExampleModel>(relmQuickContext, multipleQueryOnly, ThrowException: true);
            dataObjectsOnly = relmContext.GetDataObjects<ExampleModel>(multipleQueryOnly, ThrowException: true);
            dataObjectsOnly = relmQuickContext.GetDataObjects<ExampleModel>(multipleQueryOnly, ThrowException: true);

            // Example usage to get multiple DataObjects using query and parameters
            var multipleParametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE some_column = @some_value
                LIMIT 10;";
            
            var multipleExampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            var dataObjects = RelmHelper.GetDataObjects<ExampleModel>(relmContext, multipleParametersQuery, Parameters: multipleExampleParameters, ThrowException: true);
            dataObjects = RelmHelper.GetDataObjects<ExampleModel>(relmQuickContext, multipleParametersQuery, Parameters: multipleExampleParameters, ThrowException: true);
            dataObjects = relmContext.GetDataObjects<ExampleModel>(multipleParametersQuery, Parameters: multipleExampleParameters, ThrowException: true);
            dataObjects = relmQuickContext.GetDataObjects<ExampleModel>(multipleParametersQuery, Parameters: multipleExampleParameters, ThrowException: true);
        }
    }
}
