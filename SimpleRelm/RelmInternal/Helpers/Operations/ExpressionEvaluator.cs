using SimpleRelm.Attributes;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Helpers.Operations
{
    public class ExpressionEvaluator
    {
        public enum Command
        {
            Where,
            Reference,
            OrderBy,
            OrderByDescending,
            Set,
            GroupBy,
            Limit,
            DistinctBy,
            Count
        }

        private bool HasWhere = false;
        private bool HasOrderBy = false;
        private bool HasGroupBy = false;

        private readonly Dictionary<string, string> UnderscoreProperties;
        private readonly Dictionary<string, string> UsedTableAliases;

        public ExpressionEvaluator(string TableName, Dictionary<string, string> UnderscoreProperties, Dictionary<string, string> UsedTableAliases = null)
        {
            this.UnderscoreProperties = UnderscoreProperties;

            this.UsedTableAliases = UsedTableAliases ?? new Dictionary<string, string> { [TableName] = "a" }; // reserve 'a' for the main table
        }

        private string GetTableAlias(string PropertyName)
        {
            if (UsedTableAliases.ContainsKey(PropertyName))
                return UsedTableAliases[PropertyName];

            var aliasCount = UsedTableAliases.Count;
            var currentAlias = string.Concat(Enumerable.Repeat(((char)((aliasCount % 26) + 97)).ToString(), (int)(aliasCount / 26.0) + 1));

            UsedTableAliases.Add(UnderscoreProperties[PropertyName], currentAlias);

            return string.Empty;
        }

        private string GenerateParameterName(string FieldName, Dictionary<string, object> QueryParameters)
        {
            var parameterName = $"@_{FieldName}_";

            if (QueryParameters.ContainsKey(parameterName))
            {
                var duplicateCount = 0;

                while (QueryParameters.ContainsKey($"{parameterName}{duplicateCount++}")) ;

                parameterName += duplicateCount.ToString() + "_";
            }

            return parameterName;
        }

        public string EvaluateWhere(KeyValuePair<Command, List<Expression>> CommandExpression, Dictionary<string, object> QueryParameters, bool GiveCommandPrefix = true, ExpressionType NodeType = ExpressionType.And)
        {
            var expression = new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(
                CommandExpression.Key,
                CommandExpression.Value
                    .Select(x => new Tuple<Expression, ICollection<ParameterExpression>>(((LambdaExpression)x).Body, ((LambdaExpression)x).Parameters))
                    .ToList());

            return EvaluateWhereExpression(expression, QueryParameters, giveCommandPrefix: GiveCommandPrefix, nodeType: NodeType) + ")";
        }

        private object ResolveParameter(Expression resolvableExpression, Dictionary<string, object> queryParameters, string parameterName)
        {
            var parameterValue = ExpressionUtilities.GetValue(resolvableExpression);

            queryParameters.Add(parameterName, resolvableExpression.Type == typeof(bool)
                ? ((bool)parameterValue ? 1 : 0)
                : parameterValue);

            return parameterValue;
        }

        private string EvaluateWhereExpression(KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>> commandExpression, Dictionary<string, object> queryParameters, bool giveCommandPrefix = true, ExpressionType nodeType = ExpressionType.And)
        {
            var findQuery = string.Empty;

            foreach (var command in commandExpression.Value)
            {
                // used when calling recursive
                if (giveCommandPrefix)
                {
                    findQuery += HasWhere
                        ? (nodeType == ExpressionType.Or || nodeType == ExpressionType.OrElse
                            ? " ) OR ( "
                            : " AND ")
                        : " WHERE (";
                
                    HasWhere = true;
                }

                if (command.Item1 is BinaryExpression binaryExpression)
                {
                    var fieldName = default(string);
                    var parameterName = default(string);
                    var currentAlias = default(string);
                    var parameterValue = default(object);

                    // NOTE: Very important to keep the order of these if statements, as the result are used in the next if statements

                    // get parameter names and values
                    if (binaryExpression.Left is MemberExpression memberExpressionLeft)
                    {
                        if (memberExpressionLeft.Expression.NodeType == ExpressionType.Constant)
                            parameterValue = ResolveParameter(memberExpressionLeft, queryParameters, parameterName);
                        else
                        {
                            fieldName = memberExpressionLeft.Member.Name;
                            parameterName = GenerateParameterName(memberExpressionLeft.Member.Name, queryParameters);

                            currentAlias = GetTableAlias(((RelmTable)memberExpressionLeft.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);
                        }
                        /*
                        (currentAlias, fieldName, parameterName, parameterValue) = ResolveMemberExpression(memberExpressionLeft, queryParameters, parameterName);
                        */
                    }

                    if (binaryExpression.Right is MemberExpression memberExpressionRight)
                    {
                        if (memberExpressionRight.Expression.NodeType == ExpressionType.Constant)
                            parameterValue = ResolveParameter(memberExpressionRight, queryParameters, parameterName);
                        else
                        {
                            fieldName = memberExpressionRight.Member.Name;
                            parameterName = GenerateParameterName(memberExpressionRight.Member.Name, queryParameters);

                            currentAlias = GetTableAlias(((RelmTable)memberExpressionRight.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);
                        }
                        /*
                        (currentAlias, fieldName, parameterName, parameterValue) = ResolveMemberExpression(memberExpressionRight, queryParameters, parameterName);
                        */
                    }

                    // evaluate binary and method expressions recursively, otherwise get the parameter name and value
                    var leftBinaryQuery = string.Empty;
                    if (binaryExpression.Left is BinaryExpression subBinaryExpressionLeft)
                        leftBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(subBinaryExpressionLeft, command.Item2) }), queryParameters, giveCommandPrefix: false);
                    else if (binaryExpression.Left is MethodCallExpression methodCallExpressionLeft)
                    {
                        // if straight method call with no member expressions, then it's a constant value, otherwise run full resolve
                        if (!methodCallExpressionLeft.Arguments.Any(x => x is MemberExpression))
                            parameterValue = ResolveParameter(methodCallExpressionLeft, queryParameters, parameterName);
                        else
                            leftBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCallExpressionLeft, command.Item2) }), queryParameters, giveCommandPrefix: false);
                    }

                    var rightBinaryQuery = string.Empty;
                    if (binaryExpression.Right is BinaryExpression subBinaryExpressionRight)
                        rightBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(subBinaryExpressionRight, command.Item2) }), queryParameters, nodeType: binaryExpression.NodeType);
                    else if (binaryExpression.Right is MethodCallExpression methodCallExpressionRight)
                    {
                        if (!methodCallExpressionRight.Arguments.Any(x => x is MemberExpression))
                            parameterValue = ResolveParameter(methodCallExpressionRight, queryParameters, parameterName);
                        else
                            rightBinaryQuery = EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(methodCallExpressionRight, command.Item2) }), queryParameters, nodeType: binaryExpression.NodeType);
                    }

                    // resolve all other parameters not already resolved
                    if (binaryExpression.Left is UnaryExpression
                        || binaryExpression.Left is NewExpression
                        || binaryExpression.Left is ConstantExpression)
                    {
                        if (binaryExpression.Left is UnaryExpression unaryExpressionLeft && unaryExpressionLeft.Operand is MethodCallExpression)
                            leftBinaryQuery += EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(unaryExpressionLeft.Operand, command.Item2) }), queryParameters, giveCommandPrefix: false, nodeType: unaryExpressionLeft.NodeType);
                        else
                            parameterValue = ResolveParameter(binaryExpression.Left, queryParameters, parameterName);
                    }

                    if (binaryExpression.Right is UnaryExpression
                        || binaryExpression.Right is NewExpression
                        || binaryExpression.Right is ConstantExpression)
                    {
                        if (binaryExpression.Right is UnaryExpression unaryExpressionRight && unaryExpressionRight.Operand is MethodCallExpression)
                            rightBinaryQuery += EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(unaryExpressionRight.Operand, command.Item2) }), queryParameters, nodeType: unaryExpressionRight.NodeType);
                        else
                            parameterValue = ResolveParameter(binaryExpression.Right, queryParameters, parameterName);
                    }

                    // build the query
                    if (!string.IsNullOrWhiteSpace(leftBinaryQuery))
                        findQuery += leftBinaryQuery;
                    else
                    {
                        if (!UnderscoreProperties.ContainsKey(fieldName))
                            throw new Exception($"No field named '{fieldName}' with attribute [RelmColumn] found.");

                        findQuery += " ";
                        findQuery += currentAlias;
                        findQuery += ".`";
                        findQuery += UnderscoreProperties[fieldName];
                        findQuery += "` ";

                        switch (binaryExpression.NodeType)
                        {
                            case ExpressionType.Equal:
                                findQuery += "=";
                                break;
                            case ExpressionType.GreaterThan:
                                findQuery += ">";
                                break;
                            case ExpressionType.GreaterThanOrEqual:
                                findQuery += ">=";
                                break;
                            case ExpressionType.LessThan:
                                findQuery += "<";
                                break;
                            case ExpressionType.LessThanOrEqual:
                                findQuery += "<=";
                                break;
                            case ExpressionType.NotEqual:
                                findQuery += "<>";
                                break;
                        }
                    }

                    findQuery += " ";
                    findQuery += parameterName;
                    findQuery += " ";

                    if (!string.IsNullOrWhiteSpace(rightBinaryQuery))
                        findQuery += rightBinaryQuery;
                }
                else if (command.Item1 is MethodCallExpression methodCall)
                {
                    var referencedMember = methodCall.Arguments.LastOrDefault(x => x is MemberExpression) as MemberExpression;
                    var parameterName = referencedMember == null ? default : GenerateParameterName(referencedMember.Member.Name, queryParameters);
                    var parameterValues = methodCall
                        .Arguments
                        .Select(x => x is MemberExpression ? null : ExpressionUtilities.GetValue(x))
                        .Where(x => x != null)
                        .Select(x => x is IEnumerable enumerable ? enumerable.Cast<object>().ToList() : x)
                        .ToList();

                    var parameterValue = default(object);
                    var currentAlias = default(string);

                    if (methodCall.Object != null)
                    {
                        if (methodCall.Object is MemberExpression expressedMember)
                        {
                            if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                            parameterValue = parameterValues.FirstOrDefault();

                            if (methodCall.Object.Type == typeof(string))
                            {
                                if (methodCall.Method.Name == nameof(string.Contains))
                                    parameterValue = $"%{parameterValue}%";
                                else if (methodCall.Method.Name == nameof(string.StartsWith))
                                    parameterValue = $"{parameterValue}%";
                                else if (methodCall.Method.Name == nameof(string.EndsWith))
                                    parameterValue = $"%{parameterValue}";
                                else
                                    throw new NotSupportedException();
                            }

                            currentAlias = GetTableAlias(((RelmTable)expressedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                            findQuery += " ";
                            findQuery += currentAlias;
                            findQuery += ".`";
                            findQuery += UnderscoreProperties[referencedMember.Member.Name];
                            findQuery += "` ";
                            findQuery += (parameterValue is string parVal && parVal.Contains('%')) ? "LIKE" : "=";
                            findQuery += " ";
                            findQuery += parameterName;
                            findQuery += " ";
                        }
                        else if (methodCall.Object is ConstantExpression expressedConstant)
                        {
                            if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                            var constantValue = ExpressionUtilities.GetValue(expressedConstant);

                            // if constant value is an enumerable, then string join all values and add single quotes around everything, otherwise just get the value with single quotes
                            if (constantValue is IEnumerable constantValues)
                                parameterValue = string.Join(",", constantValues.Cast<object>());
                            else
                                parameterValue = constantValue.ToString();

                            currentAlias = GetTableAlias(((RelmTable)referencedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                            findQuery += " FIND_IN_SET(";
                            findQuery += currentAlias;
                            findQuery += ".`";
                            findQuery += UnderscoreProperties[referencedMember.Member.Name];
                            findQuery += "`, ";
                            findQuery += parameterName;
                            findQuery += ") ";
                        }
                    }
                    else
                    {
                        if (methodCall.Method.Name == nameof(Enumerable.Contains))
                        {
                            if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                            var parameterValueList = new List<object>();

                            foreach (var parameter in parameterValues)
                            {
                                if (parameter is IEnumerable<object> parameterList)
                                    parameterValueList.AddRange(parameterList);
                                else
                                    parameterValueList.Add(parameter);
                            }

                            parameterValue = string.Join(",", parameterValueList);

                            currentAlias = GetTableAlias(((RelmTable)referencedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                            findQuery += " FIND_IN_SET(";
                            findQuery += currentAlias;
                            findQuery += ".`";
                            findQuery += UnderscoreProperties[referencedMember.Member.Name];
                            findQuery += "`, ";
                            findQuery += parameterName;
                            findQuery += ") ";
                        }
                        else if (methodCall.Method.Name == nameof(string.IsNullOrEmpty) || methodCall.Method.Name == nameof(string.IsNullOrWhiteSpace))
                        {
                            if (!UnderscoreProperties.ContainsKey(referencedMember.Member.Name))
                                throw new Exception($"No field named '{referencedMember.Member.Name}' with attribute [RelmColumn] found.");

                            currentAlias = GetTableAlias(((RelmTable)referencedMember.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                            findQuery += " ";
                            findQuery += currentAlias;
                            findQuery += ".`";
                            findQuery += UnderscoreProperties[referencedMember.Member.Name];
                            findQuery += "` ";
                            findQuery += "IS";

                            if (nodeType == ExpressionType.Not)
                                findQuery += " NOT";

                            findQuery += " NULL ";
                        }
                        else
                            throw new NotSupportedException();
                    }

                    queryParameters.Add(parameterName, parameterValue);
                }
                else if (command.Item1 is UnaryExpression unaryExpression)
                {
                    findQuery += EvaluateWhereExpression(new KeyValuePair<Command, List<Tuple<Expression, ICollection<ParameterExpression>>>>(Command.Where, new List<Tuple<Expression, ICollection<ParameterExpression>>> { new Tuple<Expression, ICollection<ParameterExpression>>(unaryExpression.Operand, command.Item2) }), queryParameters, giveCommandPrefix: false, nodeType: unaryExpression.NodeType);
                }
            }

            return findQuery;
        }

        public string EvaluateSet(KeyValuePair<Command, List<Expression>> CommandExpression, Dictionary<string, object> QueryParameters)
        {
            var setLines = new List<string>();

            var set = CommandExpression.Value.FirstOrDefault();
            var currentAlias = GetTableAlias(((RelmTable)set.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (set is MemberExpression memberAssignment)
            {
                var parameterName = GenerateParameterName(memberAssignment.Member.Name, QueryParameters);
                var parameterValue = ExpressionUtilities.GetValue(memberAssignment.Expression);

                var queryLine = " ";
                queryLine += currentAlias;
                queryLine += ".`";
                queryLine += UnderscoreProperties[memberAssignment.Member.Name];
                queryLine += "` = ";
                queryLine += parameterName;
                queryLine += " ";

                setLines.Add(queryLine);

                QueryParameters.Add(parameterName, parameterValue);
            }
            else if (set is MemberInitExpression memberInit)
            {
                foreach (var binding in memberInit.Bindings)
                {
                    var parameterName = GenerateParameterName(binding.Member.Name, QueryParameters);
                    var parameterValue = ExpressionUtilities.GetValue(((MemberAssignment)binding).Expression);

                    var queryLine = " ";
                    queryLine += currentAlias;
                    queryLine += ".`";
                    queryLine += UnderscoreProperties[binding.Member.Name];
                    queryLine += "` = ";
                    queryLine += parameterName;
                    queryLine += " ";

                    setLines.Add(queryLine);

                    QueryParameters.Add(parameterName, parameterValue);
                }
            }
            else
                throw new NotSupportedException();

            var findQuery = " SET ";
            findQuery += string.Join(", ", setLines);
            findQuery += " ";

            return findQuery;
        }

        private string EvaluatePostProcessor(List<Expression> commandExpressionValues, bool? isDescending = null)
        {
            var findQuery = " ";

            foreach (var commandExpression in commandExpressionValues)
            {
                MemberExpression methodOperand = default;
                if (commandExpression is MemberExpression methodCall)
                    methodOperand = methodCall;
                else if (commandExpression is UnaryExpression unaryExpression)
                    methodOperand = unaryExpression.Operand as MemberExpression;

                if (methodOperand == default)
                {
                    if (commandExpression is NewArrayExpression arrayExpression)
                        findQuery += EvaluatePostProcessor(arrayExpression.Expressions.ToList(), isDescending);
                    else
                        throw new InvalidCastException();
                }
                else
                {
                    var currentAlias = GetTableAlias(((RelmTable)methodOperand.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

                    if (!(isDescending.HasValue ? HasOrderBy : HasGroupBy))
                    {
                        findQuery += $" ";
                        findQuery += isDescending.HasValue ? "ORDER" : "GROUP";
                        findQuery += $" BY ";

                        if (isDescending.HasValue)
                            HasOrderBy = true;
                        else
                            HasGroupBy = true;
                    }
                    else
                        findQuery += ", ";

                    findQuery += currentAlias;
                    findQuery += ".`";
                    findQuery += UnderscoreProperties[methodOperand.Member.Name];
                    findQuery += "` ";

                    if (isDescending.HasValue)
                        findQuery += isDescending.Value ? " DESC " : " ASC ";
                }
            }

            return findQuery;

        }

        public string EvaluateOrderBy(KeyValuePair<Command, List<Expression>> CommandExpression, bool IsDescending)
        {
            return EvaluatePostProcessor(CommandExpression.Value, IsDescending);
        }

        public string EvaluateGroupBy(KeyValuePair<Command, List<Expression>> CommandExpression)
        {
            return EvaluatePostProcessor(CommandExpression.Value);
        }

        public string EvaluateCount(KeyValuePair<Command, List<Expression>> CommandExpression)
        {
            var findQuery = string.Empty;

            var count = CommandExpression.Value.Count;

            findQuery += $" COUNT(*) ";

            return findQuery;
        }

        public string EvaluateLimit(KeyValuePair<Command, List<Expression>> CommandExpression)
        {
            return $" LIMIT {(CommandExpression.Value[0] as ConstantExpression).Value} ";
        }

        public string EvaluateDistinctBy(KeyValuePair<Command, List<Expression>> CommandExpression)
        {
            MemberExpression methodOperand;
            if (CommandExpression.Value[0] is MemberExpression methodCall)
                methodOperand = methodCall;
            else if (CommandExpression.Value[0] is UnaryExpression unaryExpression)
                methodOperand = unaryExpression.Operand as MemberExpression;
            else
                throw new InvalidCastException();

            var currentAlias = GetTableAlias(((RelmTable)methodOperand.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            var findQuery = $" DISTINCT ";
            findQuery += currentAlias;
            findQuery += ".`";
            findQuery += UnderscoreProperties[methodOperand.Member.Name];
            findQuery += "` ";

            return findQuery;
        }
    }
}
