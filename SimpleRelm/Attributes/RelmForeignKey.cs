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
        public string[] ForeignKeys { get; set; } = default;
        public string[] LocalKeys { get; set; } = default;
        public string OrderBy { get; set; } = default;

        public RelmForeignKey(string ForeignKeyProperty, string LocalKeyProperty = null, string OrderByProperty = null)
        {
            this.ForeignKeys = new string[] { ForeignKeyProperty };
            this.LocalKeys = new string[] { LocalKeyProperty };
            this.OrderBy = OrderByProperty;
        }

        public RelmForeignKey(string[] ForeignKeyProperties, string[] LocalKeyProperties = null, string OrderByProperty = null)
        {
            this.ForeignKeys = ForeignKeyProperties;
            this.LocalKeys = LocalKeyProperties;
            this.OrderBy = OrderByProperty;
        }
    }
}
