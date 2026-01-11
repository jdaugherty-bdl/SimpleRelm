using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces.RelmQuick
{
    /// <summary>
    /// Provides access to the field loading quick context for use with RELM field loaders.
    /// </summary>
    public interface IRelmQuickFieldLoader : IRelmFieldLoaderBase
    {
        /// <summary>
        /// Gets the current quick context for Relm operations.
        /// </summary>
        IRelmQuickContext RelmContext { get; }
    }
}
