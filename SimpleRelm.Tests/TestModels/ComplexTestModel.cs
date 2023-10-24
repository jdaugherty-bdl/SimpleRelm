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

        public string? ComplexReferenceObjectLocalKey { get; set; }
        
        public virtual ICollection<ComplexTestModel>? ComplexTestModels { get; set; }

        [RelmDataLoader(typeof(TestFieldBooleanFieldLoader))]
        public virtual bool? TestFieldBoolean { get; set; }

        [RelmDataLoader(typeof(TestFieldBooleansFieldLoader))]
        public virtual ICollection<bool>? TestFieldBooleans { get; set; }
        
        /********************* FOR TESTING REFERENCES *******************************/

        /**** FOREIGN KEY ****/
        public virtual ICollection<ComplexReferenceObject>? ComplexReferenceObjects { get; set; }
        public virtual ComplexReferenceObject? ComplexReferenceObject { get; set; }

        /**** NAVIGATION ENTITY ****/
        public virtual ICollection<ComplexReferenceObject_NavigationProperty>? ComplexReferenceObject_NavigationProperties { get; set; }
        public virtual ComplexReferenceObject_NavigationProperty? ComplexReferenceObject_NavigationPropertyItem { get; set; }

        /**** PRIMARY ENTITY, REMOTE KEY ****/
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelInternalId))]
        public virtual ICollection<ComplexReferenceObject_PrincipalEntity>? ComplexReferenceObject_PrincipalEntities { get; set; }
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelInternalId))]
        public virtual ComplexReferenceObject_PrincipalEntity? ComplexReferenceObject_PrincipalEntityItem { get; set; }

        /**** PRIMARY ENTITY, LOCAL KEY ****/
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelLocalKey), LocalKey = nameof(ComplexReferenceObjectLocalKey))]
        public virtual ICollection<ComplexReferenceObject_PrincipalEntity>? ComplexReferenceObject_PrincipalEntities_LocalKeys { get; set; }
        [RelmForeignKey(nameof(ComplexReferenceObject_PrincipalEntity.ComplexTestModelLocalKey), LocalKey = nameof(ComplexReferenceObjectLocalKey))]
        public virtual ComplexReferenceObject_PrincipalEntity? ComplexReferenceObject_PrincipalEntity_LocalKey { get; set; }

        /**** INVALID REFERENCE ****/
        public virtual ICollection<SimpleReferenceObject>? SimpleReferenceObjects { get; set; }
    }
}
