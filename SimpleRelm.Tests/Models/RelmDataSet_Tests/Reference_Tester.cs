using Moq;
using SimpleRelm.Interfaces;
using SimpleRelm.Tests.Interfaces;
using SimpleRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.Models.RelmDataSet_Tests
{
    public class Reference_Tester
    {
        [Fact]
        public void Reference_LoadsReferenceObjectsCorrectly()
        {
            // Arrange
            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel { InternalId = "ID1", ReferenceObjects = null },
                new ComplexTestModel { InternalId = "ID2", ReferenceObjects = null },
            };

            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID3", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID4", TestModel = null },
            };

            var mockDataSetTestModel = new Mock<IRelmDataSet_Test<ComplexTestModel>>();
            var mockDataSetReferenceObject = new Mock<IRelmDataSet_Test<ComplexReferenceObject>>();

            mockDataSetTestModel.Setup(m => m.GetLoadData()).Returns(mockComplexTestModels);
            mockDataSetReferenceObject.Setup(m => m.GetLoadData()).Returns(mockComplexReferenceObjects);
            mockDataSetTestModel.Reset();

            var context = new ComplexTestContext("name=SimpleRelmMySql")
            {
                ComplexTestModels = mockDataSetTestModel.Object,
                ComplexReferenceObjects = mockDataSetReferenceObject.Object
            };

            // Act
            context.ComplexTestModels.Load();
            context.ComplexTestModels.Reference(x => x.ReferenceObjects).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            Assert.NotNull(firstModel.ReferenceObjects);
            Assert.True(firstModel.ReferenceObjects.Any());
        }
    }
}
