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
    internal class DatabaseWorkExamples
    {
        internal void RunExamples(IRelmContext relmContext)
        {
            // Example usage to run DoDatabaseWork using query only with no return
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            RelmHelper.DoDatabaseWork(relmContext, queryOnly, ThrowException: true);
            relmContext.DoDatabaseWork(queryOnly, ThrowException: true);

            // Example usage to run DoDatabaseWork using query and parameters with no return
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };
            
            RelmHelper.DoDatabaseWork(relmContext, parametersQuery, exampleParameters, ThrowException: true);
            relmContext.DoDatabaseWork(parametersQuery, exampleParameters, ThrowException: true);

            // Example usage to run DoDatabaseWork using query only with return of number of affected rows
            var affectedRowsQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.Active)} = 0;";

            var affectedRows = RelmHelper.DoDatabaseWork<int>(relmContext, affectedRowsQuery, ThrowException: true);
            affectedRows = relmContext.DoDatabaseWork<int>(affectedRowsQuery, ThrowException: true);

            // Example usage to run DoDatabaseWork using query and parameters with return of number of affected rows
            var affectedRowsParametersQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @new_value 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";

            affectedRows = RelmHelper.DoDatabaseWork<int>(relmContext, affectedRowsParametersQuery, exampleParameters, ThrowException: true);
            affectedRows = relmContext.DoDatabaseWork<int>(affectedRowsParametersQuery, exampleParameters, ThrowException: true);
        }

        internal void RunExamples(IRelmQuickContext relmQuickContext)
        {
            // Example usage to run DoDatabaseWork using query only with no return
            var queryOnly = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()};";

            RelmHelper.DoDatabaseWork(relmQuickContext, queryOnly, ThrowException: true);
            relmQuickContext.DoDatabaseWork(queryOnly, ThrowException: true);

            // Example usage to run DoDatabaseWork using query and parameters with no return
            var parametersQuery = $@"SELECT * FROM {RelmHelper.GetDalTable<ExampleModel>()} 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";
            
            var exampleParameters = new Dictionary<string, object>
            {
                { "@guid_value", "some-guid-value" }
            };
            
            RelmHelper.DoDatabaseWork(relmQuickContext, parametersQuery, exampleParameters, ThrowException: true);
            relmQuickContext.DoDatabaseWork(parametersQuery, exampleParameters, ThrowException: true);

            // Example usage to run DoDatabaseWork using query only with return of number of affected rows
            var affectedRowsQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.Active)} = 0;";

            var affectedRows = RelmHelper.DoDatabaseWork<int>(relmQuickContext, affectedRowsQuery, ThrowException: true);
            affectedRows = relmQuickContext.DoDatabaseWork<int>(affectedRowsQuery, ThrowException: true);

            // Example usage to run DoDatabaseWork using query and parameters with return of number of affected rows
            var affectedRowsParametersQuery = $@"UPDATE {RelmHelper.GetDalTable<ExampleModel>()} 
                SET {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @new_value 
                WHERE {RelmHelper.GetColumnName<ExampleModel>(x => x.InternalId)} = @guid_value;";

            affectedRows = RelmHelper.DoDatabaseWork<int>(relmQuickContext, affectedRowsParametersQuery, exampleParameters, ThrowException: true);
            affectedRows = relmQuickContext.DoDatabaseWork<int>(affectedRowsParametersQuery, exampleParameters, ThrowException: true);
        }
    }
}
