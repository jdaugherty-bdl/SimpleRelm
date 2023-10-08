using SimpleRelm.Attributes;
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
    public class OrderByExpressionTests
    {
        private readonly ExpressionEvaluator evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, object>>? predicate;

        public OrderByExpressionTests()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestOrderByQuery_Ascending()
        {
            // Arrange
            predicate = x => x.Id;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.OrderBy, new List<Expression> { predicate.Body }), false);

            // Assert
            Assert.Equal(" ORDER BY a.`Id`  ASC ", result);
        }

        [Fact]
        public void TestOrderByQuery_Descending()
        {
            // Arrange
            predicate = x => x.Id;

            // Act
            var result = evaluator.EvaluateOrderBy(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.OrderBy, new List<Expression> { predicate.Body }), true);

            // Assert
            Assert.Equal(" ORDER BY a.`Id`  DESC ", result);
        }
    }
}
