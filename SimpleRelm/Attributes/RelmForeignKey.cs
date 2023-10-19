using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Struct)]
    public sealed class RelmForeignKey : Attribute
    {
        public string ForeignKey { get; set; } = default;
        public string LocalKey { get; set; } = default;

        public RelmForeignKey(string ForeignKeyProperty, string LocalKeyProperty = null)
        {
            this.ForeignKey = ForeignKeyProperty;
            this.LocalKey = LocalKeyProperty;
        }
    }
}
