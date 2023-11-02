using Moq;
using SimpleRelm.Models;
using SimpleRelm.Options;
using SimpleRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests
{
    public class RelmHelper_Tester
    {
        private List<ComplexTestModel>? mockComplexTestModels;

        public RelmHelper_Tester()
        {
            SetupContext(true);
        }

        private List<ComplexTestModel> SetupContext(bool haveTwoRoots = true)
        {
            // dummy data
            mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel
                {
                    InternalId = "ID1",
                    ComplexReferenceObjectLocalKey = "LOCALKEY1",
                    ComplexReferenceObjects = null,
                    ComplexReferenceObject = null,
                    ComplexReferenceObject_NavigationProperties = null,
                    ComplexReferenceObject_NavigationPropertyItem = null,
                    ComplexReferenceObject_PrincipalEntities = null,
                    ComplexReferenceObject_PrincipalEntity_LocalKey = null,
                    ComplexReferenceObject_PrincipalEntities_LocalKeys = null,
                    ComplexReferenceObject_PrincipalEntityItem = null,
                    ComplexTestModels = null,
                    SimpleReferenceObjects = null,
                    TestColumnId = default,
                    TestColumnInternalId = null,
                    TestColumnNoAttributeArguments = null,
                    TestFieldBoolean = null,
                },
            };

            if (haveTwoRoots)
                mockComplexTestModels.Add(new ComplexTestModel
                {
                    InternalId = "ID2",
                    ComplexReferenceObjectLocalKey = "LOCALKEY2",
                    ComplexReferenceObjects = null,
                    ComplexReferenceObject = null,
                    ComplexReferenceObject_NavigationProperties = null,
                    ComplexReferenceObject_NavigationPropertyItem = null,
                    ComplexReferenceObject_PrincipalEntities = null,
                    ComplexReferenceObject_PrincipalEntity_LocalKey = null,
                    ComplexReferenceObject_PrincipalEntities_LocalKeys = null,
                    ComplexReferenceObject_PrincipalEntityItem = null,
                    ComplexTestModels = null,
                    SimpleReferenceObjects = null,
                    TestColumnId = default,
                    TestColumnInternalId = null,
                    TestColumnNoAttributeArguments = null,
                    TestFieldBoolean = null,
                });

            return mockComplexTestModels;
        }

        private Mock<RelmDefaultDataLoader<ComplexReferenceObject>> SetupSingleReturnReferenceDataLoader(bool addSecondId, bool haveTwoRoots)
        {
            var mockComplexReferenceObjects = new List<ComplexReferenceObject>
            {
                new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null },
            };

            if (haveTwoRoots)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID2", TestModel = null });

            if (addSecondId)
                mockComplexReferenceObjects.Add(new ComplexReferenceObject { ComplexTestModelInternalId = "ID1", TestModel = null });

            var referenceDataLoader = new Mock<RelmDefaultDataLoader<ComplexReferenceObject>>();
            referenceDataLoader.Setup(x => x._tableName).Returns("nothing_table");
            referenceDataLoader.Setup(x => x.GetLoadData()).CallBase();
            referenceDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexReferenceObjects);

            return referenceDataLoader;
        }

        [Fact]
        public void RelmHelper_LoadForeignKeyField_SingleReturn_ComplexForeignObject()
        {
            // Arrange
            SetupContext(false);
            var modelDataLoader = SetupSingleReturnReferenceDataLoader(false, false);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(new ComplexTestContext().ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject, modelDataLoader.Object);

            // Assert
            Assert.Equal(1, loadedResults.Count);
            Assert.NotNull(loadedResults?.FirstOrDefault()?.ComplexReferenceObject);
            Assert.NotNull(mockComplexTestModels?.FirstOrDefault()?.ComplexReferenceObject);
            Assert.Equal(mockComplexTestModels?.FirstOrDefault()?.InternalId, loadedResults?.FirstOrDefault()?.ComplexReferenceObject?.ComplexTestModelInternalId);
        }

        [Fact]
        public void RelmHelper_LoadForeignKeyFields_SingleReturn_ComplexForeignObject()
        {
            // Arrange
            var modelDataLoader = SetupSingleReturnReferenceDataLoader(true, true);

            // Act
            var loadedResults = RelmHelper.LoadForeignKeyField(new ComplexTestContext().ContextOptions, mockComplexTestModels, x => x.ComplexReferenceObject, modelDataLoader.Object);

            // Assert
            Assert.Equal(1, loadedResults.Count);
            Assert.NotNull(loadedResults?.FirstOrDefault()?.ComplexReferenceObject);
            Assert.NotNull(mockComplexTestModels?.FirstOrDefault()?.ComplexReferenceObject);


            var firstModel = mockComplexTestModels.First();
            var secondModel = mockComplexTestModels.Skip(1).First();

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
        public void RelmHelper_LoadDataLoaderField_Single_Boolean()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel();

            // Act
            RelmHelper.LoadDataLoaderField(complexTestModel, x => x.TestFieldBoolean);

            // Assert
            Assert.NotNull(complexTestModel.TestFieldBoolean);
            Assert.True(complexTestModel.TestFieldBoolean);
        }

        [Fact]
        public void RelmHelper_LoadDataLoadersField_Multiple_Boolean()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel();

            // Act
            RelmHelper.LoadDataLoaderField(complexTestModel, x => x.TestFieldBooleans);

            // Assert
            Assert.Equal(4, complexTestModel?.TestFieldBooleans?.Count);

            // true, false, true, false
            Assert.True(complexTestModel?.TestFieldBooleans?.FirstOrDefault());
            Assert.False(complexTestModel?.TestFieldBooleans?.Skip(1).FirstOrDefault());
            Assert.True(complexTestModel?.TestFieldBooleans?.Skip(2).FirstOrDefault());
            Assert.False(complexTestModel?.TestFieldBooleans?.Skip(3).FirstOrDefault());
        }
    }
}
