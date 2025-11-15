using SimpleRelm.Attributes;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Quickstart.Models
{
    [RelmTable("example_models")]
    internal class ExampleModel : RelmModel
    {
        [RelmColumn]
        public string GroupInternalId { get; set; } // Column: group_InternalId

        [RelmColumn]
        public string ModelName { get; set; } // Column: model_name

        [RelmColumn]
        public int ModelIndex { get; set; } // Column: model_index

        [RelmColumn("bool_column")]
        public bool IsBoolColumn { get; set; } // Column: bool_column

        [RelmForeignKey(ForeignKey: nameof(ExampleGroup.InternalId), LocalKey: nameof(GroupInternalId))]
        public virtual ExampleGroup Group { get; set; }
    }
}
