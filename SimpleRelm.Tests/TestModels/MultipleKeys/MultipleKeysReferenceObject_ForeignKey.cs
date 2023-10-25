using SimpleRelm.Attributes;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels.MultipleKeys
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class MultipleKeysReferenceObject_ForeignKey : RelmModel
    {
        [RelmColumn]
        [RelmForeignKey(nameof(MultipleKeysTestObject.MultipleKeysReferenceObjectLocalKey1), nameof(MultipleKeysTestObject_Reference))]
        public string? ReferenceKey1 { get; set; }
        [RelmColumn]
        [RelmForeignKey(nameof(MultipleKeysTestObject.MultipleKeysReferenceObjectLocalKey2), nameof(MultipleKeysTestObject_Reference))]
        public string? ReferenceKey2 { get; set; }

        public MultipleKeysTestObject? MultipleKeysTestObject_Reference { get; set; }
    }
}
