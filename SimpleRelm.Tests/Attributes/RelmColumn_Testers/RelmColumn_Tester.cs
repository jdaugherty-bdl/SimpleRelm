using SimpleRelm.Attributes;
using SimpleRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.Attributes.RelmColumn_Testers
{
    public class RelmColumn_Tester
    {
        [Fact]
        public void RelmColumn_ComplexTestModel_HasAtLeastOne_Attribute()
        {
            var complexTestModel = new ComplexTestModel();
            var complexTestModelType = complexTestModel.GetType();
            var complexTestModelProperties = complexTestModelType.GetProperties();

            var relmColumnAttributes = complexTestModelProperties
                .SelectMany(x => x.GetCustomAttributes(typeof(RelmColumn), true))
                .Cast<RelmColumn>();

            Assert.True(relmColumnAttributes.Any());
        }
    }
}
