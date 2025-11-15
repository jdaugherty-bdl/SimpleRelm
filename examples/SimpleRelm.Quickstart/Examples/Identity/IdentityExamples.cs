using SimpleRelm.Interfaces;
using SimpleRelm.Interfaces.RelmQuick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Examples.Identity
{
    internal class IdentityExamples
    {
        internal void RunExamples(IRelmContext relmContext, IRelmQuickContext relmQuickContext)
        {
            // Example usage to get the last inserted ID
            var lastInsertId = RelmHelper.GetLastInsertId(relmContext);
            lastInsertId = RelmHelper.GetLastInsertId(relmQuickContext);
            lastInsertId = relmContext.GetLastInsertId();
            lastInsertId = relmQuickContext.GetLastInsertId();

            // Example usage to get ID from InternalId
            var tableName = "ExampleTable";
            var internalId = "some-guid-value";

            var idFromInternalId = RelmHelper.GetIdFromInternalId(relmContext, tableName, internalId);
            idFromInternalId = RelmHelper.GetIdFromInternalId(relmQuickContext, tableName, internalId);
            idFromInternalId = relmContext.GetIdFromInternalId(tableName, internalId);
            idFromInternalId = relmQuickContext.GetIdFromInternalId(tableName, internalId);
        }
    }
}
