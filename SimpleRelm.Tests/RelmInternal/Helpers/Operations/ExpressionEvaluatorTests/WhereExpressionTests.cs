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
    public class WhereExpressionTests
    {
        private readonly ExpressionEvaluator evaluator;
        private readonly Dictionary<string, object> queryParameters;
        private Expression<Func<ComplexTestModel, bool>>? predicate;

        public WhereExpressionTests()
        {
            var tableName = typeof(ComplexTestModel).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();
            var underscoreProperties = DataNamingHelper.GetUnderscoreProperties<ComplexTestModel>(true).ToDictionary(x => x.Value.Item1, x => x.Key);

            evaluator = new ExpressionEvaluator(tableName, underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [tableName] = "a" });

            queryParameters = new();
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_Int()
        {
            // Arrange
            predicate = x => x.Id == 3L;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  a.`Id` = @_Id_ ", result);

            Assert.Equal(queryParameters["@_Id_"], 3L);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_String()
        {
            // Arrange
            predicate = x => x.InternalId == "00000000-0000-0000-0000-000000000000";

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  a.`InternalId` = @_InternalId_ ", result);

            Assert.Equal(queryParameters["@_InternalId_"], "00000000-0000-0000-0000-000000000000");
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_DateTime()
        {
            // Arrange
            predicate = x => x.CreateDate == new DateTime(2021, 1, 1);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  a.`Create_Date` = @_CreateDate_ ", result);

            Assert.Equal(queryParameters["@_CreateDate_"], new DateTime(2021, 1, 1));
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_Boolean()
        {
            // Arrange
            predicate = x => x.Active == true;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  a.`Active` = @_Active_ ", result);

            Assert.Equal(queryParameters["@_Active_"], 1);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_ListContainsField()
        {
            var objectList = new List<ComplexTestModel>
            {
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000000" },
                new ComplexTestModel { TestColumnInternalId = "00000000-0000-0000-0000-000000000001" }
            };

            // Arrange
            predicate = x => objectList.Select(y => y.TestColumnInternalId).Contains(x.InternalId);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  FIND_IN_SET(a.`InternalId`, @_InternalId_) ", result);

            Assert.Equal(queryParameters["@_InternalId_"], "00000000-0000-0000-0000-000000000000,00000000-0000-0000-0000-000000000001");
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_Equals_4Types()
        {
            // Arrange
            predicate = x => x.Id == 3L && x.InternalId == "00000000-0000-0000-0000-000000000000" && x.CreateDate == new DateTime(2021, 1, 1) && x.Active == true;

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  ( ( ( a.`Id` = @_Id_  AND  a.`InternalId` = @_InternalId_ ) AND  a.`Create_Date` = @_CreateDate_ ) AND  a.`Active` = @_Active_ )", result);

            Assert.Equal(queryParameters["@_Id_"], 3L);
            Assert.Equal(queryParameters["@_InternalId_"], "00000000-0000-0000-0000-000000000000");
            Assert.Equal(queryParameters["@_CreateDate_"], new DateTime(2021, 1, 1));
            Assert.Equal(queryParameters["@_Active_"], 1);
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_Equalities_GreaterThanOrEqual_DateTime()
        {
            // Arrange
            predicate = x => x.CreateDate >= new DateTime(2021, 1, 1);

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  a.`Create_Date` >= @_CreateDate_ ", result);

            Assert.Equal(queryParameters["@_CreateDate_"], new DateTime(2021, 1, 1));
        }

        [Fact]
        public void TestExpressionEvaluatorWhere_MethodCallExpression()
        {
            var originalDate = new DateTime(2021, 1, 1);
            var expectedDate = originalDate.AddMinutes(-15);

            // Arrange
            predicate = x => x.CreateDate >= originalDate.AddMinutes(-15); // make originalDate.AddMinutes instead of expectedDate so we get a MethodCallExpression

            // Act
            var result = evaluator.EvaluateWhere(new KeyValuePair<ExpressionEvaluator.Command, List<Expression>>(ExpressionEvaluator.Command.Where, new List<Expression> { predicate }), queryParameters);

            // Assert
            Assert.Equal(" WHERE  a.`Create_Date` >= @_CreateDate_ ", result);

            Assert.Equal(queryParameters["@_CreateDate_"], expectedDate);
        }
    }
}
