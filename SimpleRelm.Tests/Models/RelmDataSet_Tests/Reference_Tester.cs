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
        private ComplexTestContext context;

        public Reference_Tester() 
        {
            // dummy data
            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel { InternalId = "ID1", ComplexReferenceObjects = null },
                new ComplexTestModel { InternalId = "ID2", ComplexReferenceObjects = null },
            };

            context = new ComplexTestContext("name=SimpleRelmMySql");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<DefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);
            
            context.ComplexTestModels!.SetDataLoader(modelDataLoader.Object);
        }

        private void SetupReferenceDataLoader()
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            var referenceDataLoader = new Mock<DefaultDataLoader<ComplexReferenceObject>>();
            referenceDataLoader.Setup(x => x.GetLoadData()).CallBase();
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects);

            context.ComplexReferenceObjects!.SetDataLoader(referenceDataLoader.Object);
        }

        private void SetupNavigationDataLoader()
        {
            var mockComplexReferenceObjects_Navigation = new List<ComplexReferenceObject_NavigationProperty>
            {
                new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            var navigationDataLoader = new Mock<DefaultDataLoader<ComplexReferenceObject_NavigationProperty>>();
            navigationDataLoader.Setup(x => x.GetLoadData()).CallBase();
            navigationDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Navigation);

            context.ComplexReferenceObject_NavigationProperties!.SetDataLoader(navigationDataLoader.Object);
        }

        private void SetupPrincipalDataLoader()
        {
            var mockComplexReferenceObjects_Principal = new List<ComplexReferenceObject_PrincipalEntity>
            {
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            var principalDataLoader = new Mock<DefaultDataLoader<ComplexReferenceObject_PrincipalEntity>>();
            principalDataLoader.Setup(x => x.GetLoadData()).CallBase();
            principalDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Principal);

            context.ComplexReferenceObject_PrincipalEntities!.SetDataLoader(principalDataLoader.Object);
        }

        [Fact]
        public void Reference_DoesNotLoadsReferenceObjectsCorrectly_ThrowsException()
        {
            // Arrange
            SetupReferenceDataLoader();

            // Act & Assert
            Assert.Throws<MemberAccessException>(() => context.ComplexTestModels!.Reference(x => x.SimpleReferenceObjects).Load());
        }

        [Fact]
        public void Reference_LoadsReferenceObjectsCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader();

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObjects).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObjects);
            Assert.NotNull(secondModel?.ComplexReferenceObjects);

            Assert.True(firstModel.ComplexReferenceObjects.Any());
            Assert.True(secondModel.ComplexReferenceObjects.Any());

            Assert.Equal(2, firstModel.ComplexReferenceObjects.Count);
            Assert.Equal(1, secondModel.ComplexReferenceObjects.Count);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObjects?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondModel.InternalId, secondModel.ComplexReferenceObjects?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsReferenceObjectCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader();

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }
    }
}
