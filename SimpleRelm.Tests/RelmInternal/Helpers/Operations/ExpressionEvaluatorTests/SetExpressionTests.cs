using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Helpers.Operations;
using SimpleRelm.Tests.TestModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Tests.RelmInternal.Helpers.Operations.ExpressionEvaluatorTests
{
    public class SetExpressionTests
    {
        private readonly ExpressionEvaluator evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, ComplexTestModel>>? predicate;

        public SetExpressionTests()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestExpressionEvaluatorSet_String()
        {
            // Arrange
            predicate = x => new ComplexTestModel { TestColumnInternalId = "TEST_VALUE" };

            // Act
            var result = evaluator.EvaluateSet(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Set, 
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Set, predicate.Body) })
                , queryParameters);

            // Assert
            Assert.Equal(" SET  a.`test_column_InternalId` = @_TestColumnInternalId_1_  ", result);

            Assert.Equal(queryParameters["@_TestColumnInternalId_1_"], "TEST_VALUE");
        }

        [Fact]
        public void TestExpressionEvaluatorSet_Bool()
        {
            // Arrange
            predicate = x => new ComplexTestModel { Active = false };

            // Act
            var result = evaluator.EvaluateSet(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Set, 
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Set, predicate.Body) })
                , queryParameters);

            // Assert
            Assert.Equal(" SET  a.`Active` = @_Active_1_  ", result);

            Assert.Equal(queryParameters["@_Active_1_"], false);
        }

        [Fact]
        public void TestExpressionEvaluatorSet_Nothing_ThrowsNotSupportedException()
        {
            // Arrange
            predicate = x => new ComplexTestModel();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => evaluator.EvaluateSet(new KeyValuePair<ExpressionEvaluator.Command, List<IRelmExecutionCommand>>(
                    ExpressionEvaluator.Command.Set, 
                    new List<IRelmExecutionCommand> { new RelmExecutionCommand(ExpressionEvaluator.Command.Set, predicate.Body) })
                , queryParameters));
        }
    }
}
