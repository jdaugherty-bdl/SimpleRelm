using SimpleRelm.Attributes;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Models
{
    [RelmTable("example_groups")]
    internal class ExampleGroup : RelmModel
    {
        [RelmColumn]
        public string GroupName { get; set; } // Column: group_name

        [RelmForeignKey(ForeignKey: nameof(ExampleModel.GroupInternalId), LocalKey: nameof(InternalId))]
        public virtual ICollection<ExampleModel> ExampleModels { get; set; }
    }
}
