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
    internal class DataListExamples
    {
        internal void RunExamples(IRelmContext relmContext, IRelmQuickContext relmQuickContext)
        {
            // Example usage to get a DataList using query only
            var queryOnly = $@"SELECT ID FROM {RelmHelper.GetDalTable<ExampleModel>()};";
            
            var dataList = RelmHelper.GetDataList<int>(relmContext, queryOnly, ThrowException: true);
            dataList = RelmHelper.GetDataList<int>(relmQuickContext, queryOnly, ThrowException: true);
            dataList = relmContext.GetDataList<int>(queryOnly, ThrowException: true);
            dataList = relmQuickContext.GetDataList<int>(queryOnly, ThrowException: true);
            
            // Example usage to get a DataList using query and parameters
            var parametersQuery = $@"SELECT ID FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE some_column = @some_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@some_value", 12345 }
            };

            dataList = RelmHelper.GetDataList<int>(relmContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataList = RelmHelper.GetDataList<int>(relmQuickContext, parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataList = relmContext.GetDataList<int>(parametersQuery, Parameters: exampleParameters, ThrowException: true);
            dataList = relmQuickContext.GetDataList<int>(parametersQuery, Parameters: exampleParameters, ThrowException: true);
        }
    }
}
