using MySql.Data.MySqlClient;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.Options;
using SimpleRelm.Quickstart.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Contexts
{
    internal class ExampleContext : RelmContext
    {
        public ExampleContext(bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0) : base("name=ExampleContextDatabase", autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds) { }
        public ExampleContext(Enum connectionStringType, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0) : base(connectionStringType, autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds) { }
        public ExampleContext(MySqlConnection connection, MySqlTransaction transaction) : base(connection: connection, transaction) { }
        public ExampleContext(RelmContextOptionsBuilder builder) : base(builder) { }
        public ExampleContext(IRelmContext relmContext) : base(relmContext) { }

        public IRelmDataSet<ExampleModel> ExampleModels { get; set; }
        public IRelmDataSet<ExampleGroup> ExampleGroups { get; set; }
    }
}
