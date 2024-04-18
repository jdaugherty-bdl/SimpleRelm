using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace SimpleRelm.Interfaces
{
    public interface IRelmModel
    {
        long? Id { get; set; }
        bool Active { get; set; }
        string InternalId { get; set; }
        DateTime CreateDate { get; set; }
        DateTime LastUpdated { get; set; }
        IRelmModel ResetCoreAttributes(bool NullInternalId = false);
        IRelmModel ResetWithData(DataRow ModelData, string AlternateTableName = null);
        List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(bool GetOnlyDbResolvables = true);
        int WriteToDatabase(Enum ConnectionStringType, int BatchSize = 10, bool AllowAutoIncrementColumns = false);
        int WriteToDatabase(MySqlConnection ExistingConnection, MySqlTransaction SqlTransaction = null, int BatchSize = 10, bool AllowAutoIncrementColumns = false);
        dynamic GenerateDTO(IEnumerable<string> IncludeProperties = null, IEnumerable<string> ExcludeProperties = null, string SourceObjectName = null, Func<IRelmModel, Dictionary<string, object>> GetAdditionalObjectProperties = null, int Iteration = 0);
    }
}
