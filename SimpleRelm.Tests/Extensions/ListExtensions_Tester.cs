using SimpleRelm.RelmInternal.Extensions;
using SimpleRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.Extensions
{
    public class ListExtensions_Tester
    {
        [Fact]
        public void FlattenTreeObject_List_ComplexTestModel()
        {
            var testContext = new List<ComplexTestModel>()
            {
                new ComplexTestModel()
                {
                    Id = 1,
                    ComplexTestModels = new List<ComplexTestModel>()
                    {
                        new ComplexTestModel()
                        {
                            Id = 2,
                            ComplexTestModels = new List<ComplexTestModel>()
                            {
                                new ComplexTestModel()
                                {
                                    Id = 3,
                                    ComplexTestModels = new List<ComplexTestModel>()
                                    {
                                        new ComplexTestModel()
                                        {
                                            Id = 4,
                                        }
                                    }
                                },
                                new ComplexTestModel()
                                {
                                    Id = 5,
                                }
                            }
                        },
                        new ComplexTestModel()
                        {
                            Id = 6,
                        }
                    }
                }
            };
            var flattened = testContext.FlattenTreeObject(x => x.ComplexTestModels).ToList();

            Assert.Equal(1, flattened[0].Id);
            Assert.Equal(2, flattened[1].Id);
            Assert.Equal(3, flattened[2].Id);
            Assert.Equal(4, flattened[3].Id);
            Assert.Equal(5, flattened[4].Id);
            Assert.Equal(6, flattened[5].Id);

            Assert.Equal(6, flattened.Count);
        }

        [Fact]
        public void FlattenTreeObject_NullList_ComplexTestModel()
        {
            var testContext = new List<ComplexTestModel>()
            {
                new ComplexTestModel()
                {
                    Id = 1,
                    ComplexTestModels = null
                }
            };
            var flattened = testContext.FlattenTreeObject(x => x.ComplexTestModels).ToList();

            Assert.Equal(1, flattened[0].Id);
            Assert.Single(flattened);
        }

        [Fact]
        public void FlattenTreeObject_EmptyList_ComplexTestModel()
        {
            var testContext = new List<ComplexTestModel>()
            {
                new ComplexTestModel()
                {
                    Id = 1,
                    ComplexTestModels = new List<ComplexTestModel>()
                }
            };
            var flattened = testContext.FlattenTreeObject(x => x.ComplexTestModels).ToList();

            Assert.Equal(1, flattened[0].Id);
            Assert.Single(flattened);
        }
    }
}
