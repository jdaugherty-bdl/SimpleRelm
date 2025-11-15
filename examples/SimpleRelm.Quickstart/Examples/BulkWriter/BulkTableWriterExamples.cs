using SimpleRelm.Interfaces;
using SimpleRelm.Interfaces.RelmQuick;
using SimpleRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Examples.BulkWriter
{
    internal class BulkTableWriterExamples
    {
        internal void RunExamples(IRelmContext relmContext, IRelmQuickContext relmQuickContext)
        {
            // Example usage to create a BulkTableWriter with all parameters
            var insertQuery = $@"INSERT INTO {RelmHelper.GetDalTable<ExampleModel>()} 
                ({RelmHelper.GetColumnName<ExampleModel>(x => x.GroupInternalId)}, 
                    {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelName)}, 
                    {RelmHelper.GetColumnName<ExampleModel>(x => x.ModelIndex)}) 
                VALUES 
                (@value1, @value2, @value3);";

            var insertParameters = new Dictionary<string, object>
            {
                { "@value1", "abcd" },
                { "@value2", "efgh" },
                { "@value3", "ijkl" }
            };

            var bulkWriter = RelmHelper.GetBulkTableWriter<ExampleModel>(relmContext, InsertQuery: insertQuery, UseTransaction: true, ThrowException: true, AllowAutoIncrementColumns: false, AllowPrimaryKeyColumns: false, AllowUniqueColumns: false);
            bulkWriter = RelmHelper.GetBulkTableWriter<ExampleModel>(relmQuickContext, InsertQuery: insertQuery, UseTransaction: true, ThrowException: true, AllowAutoIncrementColumns: false, AllowPrimaryKeyColumns: false, AllowUniqueColumns: false);
            bulkWriter = relmContext.GetBulkTableWriter<ExampleModel>(InsertQuery: insertQuery, UseTransaction: true, ThrowException: true, AllowAutoIncrementColumns: false, AllowPrimaryKeyColumns: false, AllowUniqueColumns: false);
            bulkWriter = relmQuickContext.GetBulkTableWriter<ExampleModel>(InsertQuery: insertQuery, UseTransaction: true, ThrowException: true, AllowAutoIncrementColumns: false, AllowPrimaryKeyColumns: false, AllowUniqueColumns: false);

            var rowsUpdated = bulkWriter.Write();
        }
    }
}
