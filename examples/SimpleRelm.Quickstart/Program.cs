using SimpleRelm.Quickstart.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Run attributes examples
            var attributesExamples = new Examples.Attributes.AttributesExamples();
            attributesExamples.RunExamples();

            // Run standard connection examples
            var standardConnectionExamples = new Examples.Connections.StandardConnectionExamples();
            standardConnectionExamples.RunExamples();

            // Relm Context initializes all datasets and reads the database to preload metadata, making some subsequent operations faster

            // Initialize the Relm context
            using (var relmContext = new ExampleContext())
            {
                // Run identity examples
                var identityExamples = new Examples.Identity.IdentityExamples();
                identityExamples.RunExamples(relmContext);

                // Run data row examples
                var dataRowExamples = new Examples.Data.DataRowExamples();
                dataRowExamples.RunExamples(relmContext);

                // Run data table examples
                var dataTableExamples = new Examples.Data.DataTableExamples();
                dataTableExamples.RunExamples(relmContext);

                // Run data object examples
                var dataObjectExamples = new Examples.Data.DataObjectExamples();
                dataObjectExamples.RunExamples(relmContext);

                // Run data list examples
                var dataListExamples = new Examples.Data.DataListExamples();
                dataListExamples.RunExamples(relmContext);
            }

            // Relm Quick Context lazy loads metadata as needed with the first operation, so some operations may be slower the first time they are run

            // Initialize the Relm Quick context
            using (var relmQuickContext = new ExampleQuickContext())
            {
                // Run identity examples
                var identityExamples = new Examples.Identity.IdentityExamples();
                identityExamples.RunExamples(relmQuickContext);

                // Run data row examples
                var dataRowExamples = new Examples.Data.DataRowExamples();
                dataRowExamples.RunExamples(relmQuickContext);

                // Run data table examples
                var dataTableExamples = new Examples.Data.DataTableExamples();
                dataTableExamples.RunExamples(relmQuickContext);

                // Run data object examples
                var dataObjectExamples = new Examples.Data.DataObjectExamples();
                dataObjectExamples.RunExamples(relmQuickContext);

                // Run data list examples
                var dataListExamples = new Examples.Data.DataListExamples();
                dataListExamples.RunExamples(relmQuickContext);
            }

            // Initialize the Relm context
            using (var relmContext = new ExampleContext(autoOpenTransaction: true))
            {
                try
                {
                    // Run database work examples
                    var databaseWorkExamples = new Examples.Data.DatabaseWorkExamples();
                    databaseWorkExamples.RunExamples(relmContext);

                    // Run bulk table write examples
                    var bulkTableWriteExamples = new Examples.BulkWriter.BulkTableWriterExamples();
                    bulkTableWriteExamples.RunExamples(relmContext);
                }
                catch (Exception ex)
                {
                    relmContext.RollbackTransactions();

                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            // Initialize the Relm Quick context
            using (var relmQuickContext = new ExampleQuickContext(autoOpenTransaction: true))
            {
                try
                {
                    // Run database work examples
                    var databaseWorkExamples = new Examples.Data.DatabaseWorkExamples();
                    databaseWorkExamples.RunExamples(relmQuickContext);

                    // Run bulk table write examples
                    var bulkTableWriteExamples = new Examples.BulkWriter.BulkTableWriterExamples();
                    bulkTableWriteExamples.RunExamples(relmQuickContext);
                }
                catch (Exception ex)
                {
                    relmQuickContext.RollbackTransactions();

                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
