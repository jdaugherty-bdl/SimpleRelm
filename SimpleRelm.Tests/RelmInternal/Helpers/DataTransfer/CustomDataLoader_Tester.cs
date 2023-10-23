using Moq;
using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
using SimpleRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.RelmInternal.Helpers.DataTransfer
{
    public class CustomDataLoader_Tester
    {
        private readonly ComplexTestContext context;

        public CustomDataLoader_Tester()
        {
            // dummy data
            var mockComplexTestModels = new List<ComplexTestModel>
            {
                new ComplexTestModel
                {
                    InternalId = "ID1",
                    TestFieldBoolean = null,
                },
                new ComplexTestModel
                {
                    InternalId = "ID2",
                    TestFieldBoolean = null,
                },
            };

            context = new ComplexTestContext("name=SimpleRelmMySql");

            // create dummy data loaders for dummy data to be placed in both relevant data sets
            var modelDataLoader = new Mock<DefaultDataLoader<ComplexTestModel>>(); // { CallBase = true };

            // make sure GetLoadData() calls base so LastExecutedCommands (required for references) gets populated
            modelDataLoader.Setup(x => x.GetLoadData()).CallBase();
            modelDataLoader.Setup(x => x.PullData(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns(mockComplexTestModels);

            context.ComplexTestModels!.SetDataLoader(modelDataLoader.Object);
        }

        [Fact]
        public void FieldLoaderAttribute_DefaultRelmKey_UsedToResolveProperty_IsSuccessful()
        {
            // Arrange & Act
            context.ComplexTestModels!.Load();

            // Assert
            var firstModel = context.ComplexTestModels.First();
            var secondModel = context.ComplexTestModels.Skip(1).First();

            Assert.True(firstModel?.TestFieldBoolean);
            Assert.False(secondModel?.TestFieldBoolean);
        }

        [Fact]
        public void DataLoaderAttribute_IsSuccessful()
        {
            // Arrange
            context.DataLoaderTestModels!.Load();


        }
    }
}
