using Moq;
using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
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

            var context = new ComplexTestContext("name=SimpleRelmMySql");

            var modelDataLoader = new Mock<DefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };
            var referenceDataLoader = new Mock<DefaultDataLoader<ComplexReferenceObject>>();

            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects);

            context.ComplexTestModels!.SetDataLoader(modelDataLoader.Object);
            context.ComplexReferenceObjects!.SetDataLoader(referenceDataLoader.Object);

            // Act
            context.ComplexTestModels!.Reference(x => x.ReferenceObjects).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            Assert.NotNull(firstModel.ReferenceObjects);
            Assert.True(firstModel.ReferenceObjects.Any());
        }
    }
}
