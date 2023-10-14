using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.Interfaces
{
    internal interface IRelmContext_TESTING : IRelmContext
    {
        void SetDataSet<T>(IRelmDataSet<T> dataSet) where T : RelmModel, new();
    }
}
