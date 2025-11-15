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
    internal class DataTableExamples
    {
        internal void RunExamples(IRelmContext relmContext)
        {
            // Example usage to get a DataTable using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            var dataTable = RelmHelper.GetDataTable(relmContext, queryOnly, ThrowException: true);
            dataTable = relmContext.GetDataTable(queryOnly, ThrowException: true);

            // Example usage to get a DataTable using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            dataTable = RelmHelper.GetDataTable(relmContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataTable = relmContext.GetDataTable(parametersQuery, Parameters: exampleParameters, ThrowException: true);
        }

        internal void RunExamples(IRelmQuickContext relmQuickContext)
        {
            // Example usage to get a DataTable using query only
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            var dataTable = RelmHelper.GetDataTable(relmQuickContext, queryOnly, ThrowException: true);
            dataTable = relmQuickContext.GetDataTable(queryOnly, ThrowException: true);

            // Example usage to get a DataTable using query and parameters
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)} = @some_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            dataTable = RelmHelper.GetDataTable(relmQuickContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataTable = relmQuickContext.GetDataTable(parametersQuery, Parameters: exampleParameters, ThrowException: true);
        }
    }
}
