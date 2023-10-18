using SimpleRelm.Attributes;
using SimpleRelm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.TestModels
{
    [RelmDatabase("test_database")]
    [RelmTable("nothing_table")]
    public class ComplexTestModel : RelmModel
    {
        [RelmColumn(ColumnName: "test_column_InternalId", ColumnSize: 255, IsNullable: false, PrimaryKey: false, Autonumber: true, Unique: true, DefaultValue: "DEFAULTVALUE", Index: "INDEX", IndexDescending: true, AllowDataTruncation: true, Virtual: true)]
        public string? TestColumnInternalId { get; set; }
        [RelmColumn(ColumnName: "test_column_id", ColumnSize: 255, IsNullable: false, PrimaryKey: false, Autonumber: true, Unique: true, DefaultValue: "DEFAULTVALUE", Index: "INDEX", IndexDescending: true, AllowDataTruncation: true, Virtual: true)]
        public int TestColumnId { get; set; }
        [RelmColumn]
        public string? TestColumnNoAttributeArguments { get; set; }

        public virtual ICollection<ComplexTestModel>? ComplexTestModels { get; set; }

        public virtual ICollection<ComplexReferenceObject>? ComplexReferenceObjects { get; set; }
        public virtual ComplexReferenceObject? ComplexReferenceObject { get; set; }

        public virtual ICollection<ComplexReferenceObject_NavigationProperty>? ComplexReferenceObject_NavigationProperties { get; set; }
        public virtual ComplexReferenceObject_NavigationProperty? ComplexReferenceObject_NavigationPropertyItem { get; set; }

        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelInternalId))]
        public virtual ICollection<ComplexReferenceObject_PrincipalEntity>? ComplexReferenceObject_PrincipalEntities { get; set; }
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelInternalId))]
        public virtual ComplexReferenceObject_PrincipalEntity? ComplexReferenceObject_PrincipalEntityItem { get; set; }
        
        public virtual ICollection<SimpleReferenceObject>? SimpleReferenceObjects { get; set; }
    }
}
