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
        public void RelmHelper_LoadForeignKey_ComplexObject()
        {
            // Arrange
            var complexTestModel = new ComplexTestModel
            {
                InternalId = "ID1"
            };

            // Act
            var ddd = RelmHelper.LoadForeignKey(null, complexTestModel, x => x.TestFieldBoolean);

            // Assert
            Assert.True(complexTestModel.TestFieldBoolean);
        }
    }
}
