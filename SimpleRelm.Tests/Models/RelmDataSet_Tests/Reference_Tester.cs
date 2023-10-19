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

        private void SetupReferenceDataLoader(bool secondId1)
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (secondId1)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null });

            var referenceDataLoader = new Mock<DefaultDataLoader<ComplexReferenceObject>>();
            referenceDataLoader.Setup(x => x.GetLoadData()).CallBase();
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects);

            context.ComplexReferenceObjects!.SetDataLoader(referenceDataLoader.Object);
        }

        private void SetupNavigationDataLoader(bool secondId1)
        {
            var mockComplexReferenceObjects_Navigation = new List<ComplexReferenceObject_NavigationProperty>
            {
                new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (secondId1)
                mockComplexReferenceObjects_Navigation.Add(new ComplexReferenceObject_NavigationProperty { ComplexTestModelInternalId = "ID1", TestModel = null });

            var navigationDataLoader = new Mock<DefaultDataLoader<ComplexReferenceObject_NavigationProperty>>();
            navigationDataLoader.Setup(x => x.GetLoadData()).CallBase();
            navigationDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Navigation);

            context.ComplexReferenceObject_NavigationProperties!.SetDataLoader(navigationDataLoader.Object);
        }

        private void SetupPrincipalDataLoader(bool secondId1)
        {
            var mockComplexReferenceObjects_Principal = new List<ComplexReferenceObject_PrincipalEntity>
            {
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID1", TestModel = null },
                new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID2", TestModel = null },
            };

            if (secondId1)
                mockComplexReferenceObjects_Principal.Add(new ComplexReferenceObject_PrincipalEntity { ComplexTestModelInternalId = "ID1", TestModel = null });

            var principalDataLoader = new Mock<DefaultDataLoader<ComplexReferenceObject_PrincipalEntity>>();
            principalDataLoader.Setup(x => x.GetLoadData()).CallBase();
            principalDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects_Principal);

            context.ComplexReferenceObject_PrincipalEntities!.SetDataLoader(principalDataLoader.Object);
        }

        [Fact]
        public void Reference_DoesNotLoadsReferenceObjectsCorrectly_ThrowsException()
        {
            // Arrange
            SetupReferenceDataLoader(false);

            // Act & Assert
            Assert.Throws<MemberAccessException>(() => context.ComplexTestModels!.Reference(x => x.SimpleReferenceObjects).Load());
        }

        [Fact]
        public void Reference_LoadsReferenceObjectsCorrectly()
        {
            // Arrange
            SetupReferenceDataLoader(true);

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
            SetupReferenceDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectsCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_NavigationProperties).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObject_NavigationProperties);
            Assert.NotNull(secondModel?.ComplexReferenceObject_NavigationProperties);

            Assert.True(firstModel.ComplexReferenceObject_NavigationProperties.Any());
            Assert.True(secondModel.ComplexReferenceObject_NavigationProperties.Any());

            Assert.Equal(2, firstModel.ComplexReferenceObject_NavigationProperties.Count);
            Assert.Equal(1, secondModel.ComplexReferenceObject_NavigationProperties.Count);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_NavigationProperties?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_NavigationProperties?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondModel.InternalId, secondModel.ComplexReferenceObject_NavigationProperties?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsNavigationPropertyObjectCorrectly()
        {
            // Arrange
            SetupNavigationDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_NavigationPropertyItem).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_NavigationPropertyItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_NavigationPropertyItem?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectsCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(true);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntities).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.NotNull(firstModel?.ComplexReferenceObject_PrincipalEntities);
            Assert.NotNull(secondModel?.ComplexReferenceObject_PrincipalEntities);

            Assert.True(firstModel.ComplexReferenceObject_PrincipalEntities.Any());
            Assert.True(secondModel.ComplexReferenceObject_PrincipalEntities.Any());

            Assert.Equal(2, firstModel.ComplexReferenceObject_PrincipalEntities.Count);
            Assert.Equal(1, secondModel.ComplexReferenceObject_PrincipalEntities.Count);

            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_PrincipalEntities?.FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(firstModel.InternalId, firstModel.ComplexReferenceObject_PrincipalEntities?.Skip(1).FirstOrDefault()?.ComplexTestModelInternalId);
            Assert.Equal(secondModel.InternalId, secondModel.ComplexReferenceObject_PrincipalEntities?.FirstOrDefault()?.ComplexTestModelInternalId);
        }

        [Fact]
        public void Reference_LoadsPrincipalEntityObjectCorrectly()
        {
            // Arrange
            SetupPrincipalDataLoader(false);

            // Act
            context.ComplexTestModels!.Reference(x => x.ComplexReferenceObject_PrincipalEntityItem).Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();

            Assert.NotNull(firstModel.ComplexReferenceObject_PrincipalEntityItem);
            Assert.Equal(firstModel.InternalId, firstModel?.ComplexReferenceObject_PrincipalEntityItem?.ComplexTestModelInternalId);
        }
    }
}
