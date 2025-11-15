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
            // below is an example of creating a context with specific options
            using (var exampleContext = new ExampleContext(autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0))
            {
                var exampleQuery = "SELECT NOW();";
                RelmHelper.GetScalar<DateTime>(exampleContext, exampleQuery);
                var result = exampleContext.GetScalar<DateTime>(exampleQuery);
            }

            using (var exampleQuickContext = new ExampleQuickContext(autoOpenConnection: true, autoOpenTransaction: false, allowUserVariables: false, convertZeroDateTime: false, lockWaitTimeoutSeconds: 0))
            {
                var exampleQuery = "SELECT NOW();";
                RelmHelper.GetScalar<DateTime>(exampleQuickContext, exampleQuery);
                var result = exampleQuickContext.GetScalar<DateTime>(exampleQuery);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
