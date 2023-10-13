using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.Interfaces
{
    public interface IRelmDataSet_Test<T> : IRelmDataSet<T> where T : IRelmModel, new()
    {
        public ICollection<T> GetLoadData();
    }
}
