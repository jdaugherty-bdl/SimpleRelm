using MySql.Data.MySqlClient;
using SimpleRelm.Models;
using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Contexts
{
    internal class ExampleQuickContext : RelmQuickContext
    {
        public ExampleQuickContext(bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0) : base("name=PortalCertDatabase", autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds) { }
        public ExampleQuickContext(Enum connectionStringType, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0) : base(connectionStringType, autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds) { }
        public ExampleQuickContext(MySqlConnection connection, MySqlTransaction transaction, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0) : base(connection: connection, transaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds) { }
        public ExampleQuickContext(RelmContextOptionsBuilder builder, bool autoOpenConnection = true, bool autoOpenTransaction = false, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0) : base(builder, autoOpenConnection: autoOpenConnection, autoOpenTransaction: autoOpenTransaction, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds) { }
    }
}
