using SimpleRelm.Interfaces;
using SimpleRelm.Interfaces.RelmQuick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Helpers.DataTransfer
{
    internal class RowIdentityHelper
    {
        /// <summary>
        /// Use the MySql built in function to get the ID of the last row inserted.
        /// </summary>
        /// <param name="ConfigConnectionString">The connection type to use when getting the last ID.</param>
        /// <returns>A string representation of the ID.</returns>
        internal static string GetLastInsertId(Enum ConfigConnectionString)
        {
            return RefinedResultsHelper.GetScalar<string>(ConfigConnectionString, "SELECT LAST_INSERT_ID();");
        }

        internal static string GetLastInsertId(IRelmContext relmContext)
        {
            return RefinedResultsHelper.GetScalar<string>(relmContext, "SELECT LAST_INSERT_ID();");
        }

        internal static string GetLastInsertId(IRelmQuickContext relmQuickContext)
        {
            return RefinedResultsHelper.GetScalar<string>(relmQuickContext, "SELECT LAST_INSERT_ID();");
        }

        /// <summary>
        /// Converts an InternalId to an autonumbered row ID.
        /// </summary>
        /// <param name="ConfigConnectionString">The connection type to use.</param>
        /// <param name="Table">Table name to use for the conversion.</param>
        /// <param name="InternalId">The GUID of the InternalId to convert.</param>
        /// <returns>ID of the row matching the InternalId.</returns>
        internal static string GetIdFromInternalId(Enum ConfigConnectionString, string Table, string InternalId)
        {
            return RefinedResultsHelper.GetScalar<string>(ConfigConnectionString, $"SELECT ID FROM {Table} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
        }

        internal static string GetIdFromInternalId(IRelmContext relmContext, string Table, string InternalId)
        {
            return RefinedResultsHelper.GetScalar<string>(relmContext, $"SELECT ID FROM {Table} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
        }

        internal static string GetIdFromInternalId(IRelmQuickContext relmQuickContext, string Table, string InternalId)
        {
            return RefinedResultsHelper.GetScalar<string>(relmQuickContext, $"SELECT ID FROM {Table} WHERE InternalId = @InternalId", new Dictionary<string, object> { { "@InternalId", InternalId } });
        }
    }
}
