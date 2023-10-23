using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class)]
    public class RelmDataLoader : Attribute
    {
        public Type LoaderType { get; set; } = default;
        public string KeyField { get; set; } = default;

        public RelmDataLoader(Type LoaderType, string KeyField = null)
        {
            this.LoaderType = LoaderType;

            if (KeyField != null)
            {
                this.KeyField = KeyField;
            }
        }
    }
}
