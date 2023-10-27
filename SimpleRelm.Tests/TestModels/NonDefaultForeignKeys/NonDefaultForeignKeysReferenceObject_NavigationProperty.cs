using SimpleRelm.Attributes;
using SimpleRelm.Models;
using SimpleRelm.Tests.TestModels.MultipleKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels.NonDefaultForeignKeys
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class NonDefaultForeignKeysReferenceObject_NavigationProperty : RelmModel
    {
        public string? ReferenceKey { get; set; }

        [RelmForeignKey(nameof(NonDefaultForeignKeysTestObject.NonDefaultForeignKeysReferenceObjectLocalKey), nameof(ReferenceKey))]
        public NonDefaultForeignKeysTestObject? NonDefaultForeignKeysTestObject_Reference { get; set; }
    }
}
