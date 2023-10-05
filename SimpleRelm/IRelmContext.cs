using SimpleRelm.Interfaces;
using SimpleRelm.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm
{
    public interface IRelmContext
    {
        RelmContextOptionsBuilder OptionsBuilder { get; }

        IRelmSet<T> GetDataSetType<T>() where T : IRelmModel, new();
        IRelmSet<T> GetDataSetType<T>(bool throwException) where T : IRelmModel, new();
        IRelmSetBase GetDataSetType(Type dataSetType);
        IRelmSetBase GetDataSetType(Type dataSetType, bool throwException);
    }
}
