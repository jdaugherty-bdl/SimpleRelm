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

        [Fact]
        public void RelmHelper_LoadForeignKeyField_Single_ComplexForeignObject()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel();

            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel
                {
                    InternalId = "ID1",
                },
            };

            var modelDataLoader = new Mock<RelmDefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x._tableName).Returns("nothing_table");
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);

            // Act
            RelmHelper.LoadForeignKeyField(new ComplexTestContext().ContextOptions, complexTestModel, x => x.ComplexReferenceObject, modelDataLoader.Object);

            // Assert
        }
    }
}
