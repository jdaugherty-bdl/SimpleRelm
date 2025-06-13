using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces
{
    public interface IRelmFieldLoader
    {
        string FieldName { get; }
        string[] KeyFields { get; }
        IRelmContext RelmContext { get; }
        Dictionary<S[], object> GetFieldData<S>(ICollection<S[]> keyData);
    }
}
