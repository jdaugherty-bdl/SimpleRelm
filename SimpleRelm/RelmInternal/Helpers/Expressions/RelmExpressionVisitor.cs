using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.RelmInternal.Helpers.Operations;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using SimpleRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.RelmInternal.Helpers.Expressions
{
    internal class RelmExpressionVisitor<T> where T : IRelmModel, new()
    {
        public Dictionary<string, object> QueryParameters { get; private set; }

        private readonly Dictionary<string, string> _underscoreProperties;
        private readonly Dictionary<string, string> _usedTableAliases;
        private readonly Dictionary<Type, Dictionary<string, string>> _objectProperties;

        internal RelmExpressionVisitor(string TableName = null, Dictionary<string, string> UnderscoreProperties = null, Dictionary<string, string> UsedTableAliases = null)
        {
            var _tableName = TableName;
            if (string.IsNullOrWhiteSpace(_tableName))
                _tableName = typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName ?? throw new ArgumentNullException();

            _underscoreProperties = UnderscoreProperties;
            if ((_underscoreProperties?.Count ?? 0) == 0)
                _underscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true, false).ToDictionary(x => x.Value.Item1, x => x.Key);

            _objectProperties = new Dictionary<Type, Dictionary<string, string>>
            {
                [typeof(T)] = UnderscoreProperties
            };

            _usedTableAliases = UsedTableAliases ?? new Dictionary<string, string> { [_tableName] = "a" }; // reserve 'a' for the main table
        }

        internal ExpressionResolution Visit(Expression expression, ExpressionResolution expressionResolution = null)
        {
            if (expression == null)
                return null;

            switch (expression.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary((UnaryExpression)expression, expressionResolution);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.VisitBinary((BinaryExpression)expression);
                /*
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)expression);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)expression);
                */
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)expression, expressionResolution);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)expression);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)expression, expressionResolution);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)expression, expressionResolution);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)expression);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)expression, expressionResolution);
                /*
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)expression);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)expression);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)expression);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)expression);
                */
                default:
                    throw new Exception($"Unhandled expression type: '{expression.NodeType}'");
            }
        }

        /*
        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return this.VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception($"Unhandled binding type '{binding.BindingType}'");
            }
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            var arguments = this.VisitExpressionList(initializer.Arguments);

            if (arguments != initializer.Arguments)
                return Expression.ElementInit(initializer.AddMethod, arguments);

            return initializer;
        }
        */

        protected virtual ExpressionResolution VisitUnary(UnaryExpression unary, ExpressionResolution expressionResolution)
        {
            var operand = this.Visit(unary.Operand, expressionResolution);

            /*
            if (operand != unary.Operand)
                return Expression.MakeUnary(unary.NodeType, operand, unary.Type, unary.Method);

            return unary;
            */
            return operand;
        }

        protected virtual ExpressionResolution VisitBinary(BinaryExpression binary)
        {
            var left = this.Visit(binary.Left);
            var right = this.Visit(binary.Right, left);
            //var conversion = this.Visit(binary.Conversion);

            var parameterName = left.ParameterName;
            var parameterValue = right.ParameterValue;
            var query = left.Query;

            if (string.IsNullOrWhiteSpace(left.ParameterName))
            {
                parameterName = right.ParameterName;
                parameterValue = left.ParameterValue;
                query = right.Query;
            }

            QueryParameters = QueryParameters ?? new Dictionary<string, object>();
            if (!QueryParameters.ContainsKey(parameterName))
                QueryParameters.Add(parameterName, parameterValue);

            return new ExpressionResolution
            {
                Query = $"{query} {ExpressionUtilities.GetSqlOperator(binary.NodeType)} {parameterName}"
            };
        }

        /*
        protected virtual ExpressionResolution VisitTypeIs(TypeBinaryExpression typeBinary)
        {
            var expr = this.Visit(typeBinary.Expression);

            if (expr != typeBinary.Expression)
                return Expression.TypeIs(expr, typeBinary.TypeOperand);

            return typeBinary;
        }
        */

        protected virtual ExpressionResolution VisitConstant(ConstantExpression constant, ExpressionResolution expressionResolution)
        {
            var constantResolution = new ExpressionResolution
            {
                ParameterValue = ExpressionUtilities.GetValue(constant)
            };

            if (constantResolution.ParameterValue.GetType() == typeof(bool))
            {
                constantResolution.Query = (bool)constantResolution.ParameterValue ? "1" : "0";
                constantResolution.ParameterValue = (bool)constantResolution.ParameterValue ? 1 : 0;
            }
            else
                constantResolution.Query = constantResolution.ParameterValue.ToString();

            return constantResolution;
        }

        /*
        protected virtual ExpressionResolution VisitConditional(ConditionalExpression conditional)
        {
            var test = this.Visit(conditional.Test);
            var ifTrue = this.Visit(conditional.IfTrue);
            var ifFalse = this.Visit(conditional.IfFalse);

            if (test != conditional.Test || ifTrue != conditional.IfTrue || ifFalse != conditional.IfFalse)
                return Expression.Condition(test, ifTrue, ifFalse);

            return conditional;
        }
        */

        protected virtual ExpressionResolution VisitParameter(ParameterExpression parameter)
        {
            var currentAlias = GetTableAlias(((RelmTable)parameter.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            if (string.IsNullOrWhiteSpace(currentAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{parameter.Type.FullName}]");

            return new ExpressionResolution {
                TableAlias = currentAlias
            };
        }

        protected virtual ExpressionResolution VisitMemberAccess(MemberExpression member, ExpressionResolution expressionResolution)
        {
            var resolution = this.Visit(member.Expression, expressionResolution);

            if (member.Expression.NodeType == ExpressionType.Constant)
                //ParameterValue = ResolveParameter(constant, expressionResolution.TableAlias, expressionResolution.ParameterName)
                resolution.ParameterValue = resolution.ParameterValue.GetType().GetField(member.Member.Name).GetValue(resolution.ParameterValue);
            else
            {
                if (!_objectProperties.ContainsKey(member.Expression.Type))
                {
                    _objectProperties[member.Expression.Type] = DataNamingHelper.GetUnderscoreProperties(member.Expression.Type, true, false).ToDictionary(x => x.Value.Item1, x => x.Key);
                }

                resolution.FieldName = member.Member.Name;
                resolution.ParameterName = GenerateParameterName(resolution);

                //resolution.Query = $"{resolution.TableAlias}.`{_underscoreProperties[resolution.FieldName]}`";
                resolution.Query = $"{resolution.TableAlias}.`{_objectProperties[member.Expression.Type][resolution.FieldName]}`";
            }

            return resolution;
        }

        protected virtual ExpressionResolution VisitMethodCall(MethodCallExpression methodCall, ExpressionResolution expressionResolution)
        {
            var obj = this.Visit(methodCall.Object, expressionResolution);
            var args = this.VisitExpressionList(methodCall.Arguments, expressionResolution);

            /*
            if (obj != methodCall.Object || args != methodCall.Arguments)
                return Expression.Call(obj, methodCall.Method, args);

            return methodCall;
            */
            return obj;
        }

        protected virtual List<object> VisitExpressionList(ReadOnlyCollection<Expression> original, ExpressionResolution expressionResolution)
        {
            var list = new List<object>();
            var originalCount = original.Count;

            for (var i = 0; i < originalCount; i++)
            {
                var currentExpression = this.Visit(original[i], expressionResolution);

                list.Add(currentExpression.ParameterValue);
            }

            return list;
        }

        /*
        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment memberAssignment)
        {
            var e = this.Visit(memberAssignment.Expression);

            if (e != memberAssignment.Expression)
                return Expression.Bind(memberAssignment.Member, e);

            return memberAssignment;
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding memberBinding)
        {
            var bindings = this.VisitBindingList(memberBinding.Bindings);

            if (bindings != memberBinding.Bindings)
                return Expression.MemberBind(memberBinding.Member, bindings);

            return memberBinding;
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding listBinding)
        {
            var initializers = this.VisitElementInitializerList(listBinding.Initializers);

            if (initializers != listBinding.Initializers)
                return Expression.ListBind(listBinding.Member, initializers);

            return listBinding;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            List<MemberBinding> list = null;
            var originalCount = original.Count;

            for (var i = 0; i < originalCount; i++)
            {
                var currentBinding = this.VisitBinding(original[i]);

                if (list != null)
                    list.Add(currentBinding);
                else if (currentBinding != original[i])
                    list = new List<MemberBinding>(originalCount);

                for (var j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(currentBinding);
            }

            if (list != null)
                return list;

            return original;
        }

        protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            List<ElementInit> list = null;
            var originalCount = original.Count;

            for (var i = 0; i < originalCount; i++)
            {
                var init = this.VisitElementInitializer(original[i]);

                if (list != null)
                    list.Add(init);
                else if (init != original[i])
                    list = new List<ElementInit>(originalCount);

                for (var j = 0; j < i; j++)
                {
                    list.Add(original[j]);
                }

                list.Add(init);
            }

            if (list != null)
                return list;

            return original;
        }
        */

        protected virtual ExpressionResolution VisitLambda(LambdaExpression lambda)
        {
            var resolution = this.Visit(lambda.Body);

            return resolution;
        }

        protected virtual ExpressionResolution VisitNew(NewExpression newExpression, ExpressionResolution expressionResolution)
        {
            var newExpressionArguments = this.VisitExpressionList(newExpression.Arguments, expressionResolution);
            var newValue = newExpression.Constructor.Invoke(newExpressionArguments.ToArray());

            var resolution = new ExpressionResolution
            {
                ParameterValue = QueryParameters[expressionResolution.ParameterName] = newValue
            };

            if (newExpression.Type == typeof(DateTime))
                resolution.Query = string.Join("-", newExpressionArguments);

            /*
            if (newExpressionArguments != newExpression.Arguments)
            {
                if (newExpression.Members != null)
                    return Expression.New(newExpression.Constructor, newExpressionArguments, newExpression.Members);
                else
                    return Expression.New(newExpression.Constructor, newExpressionArguments);
            }

            return newExpression;
            */
            return resolution;
        }
        /*

        protected virtual ExpressionResolution VisitMemberInit(MemberInitExpression memberInit)
        {
            var newExpression = this.VisitNew(memberInit.NewExpression);
            var bindings = this.VisitBindingList(memberInit.Bindings);

            if (newExpression != memberInit.NewExpression || bindings != memberInit.Bindings)
                return Expression.MemberInit(newExpression, bindings);

            return memberInit;
        }

        protected virtual ExpressionResolution VisitListInit(ListInitExpression listInit)
        {
            var newExpression = this.VisitNew(listInit.NewExpression);
            var initializers = this.VisitElementInitializerList(listInit.Initializers);

            if (newExpression != listInit.NewExpression || initializers != listInit.Initializers)
                return Expression.ListInit(newExpression, initializers);

            return listInit;
        }

        protected virtual ExpressionResolution VisitNewArray(NewArrayExpression newArray)
        {
            var expressions = this.VisitExpressionList(newArray.Expressions);

            if (expressions != newArray.Expressions)
            {
                if (newArray.NodeType == ExpressionType.NewArrayInit)
                    return Expression.NewArrayInit(newArray.Type.GetElementType(), expressions);
                else
                    return Expression.NewArrayBounds(newArray.Type.GetElementType(), expressions);
            }

            return newArray;
        }

        protected virtual ExpressionResolution VisitInvocation(InvocationExpression invocation)
        {
            var invocationArguments = this.VisitExpressionList(invocation.Arguments);
            var expression = this.Visit(invocation.Expression);

            if (invocationArguments != invocation.Arguments || expression != invocation.Expression)
                return Expression.Invoke(expression, invocationArguments);

            return invocation;
        }
        */

        private ExpressionResolution GetNamesAndAliases(MemberExpression memberExpression)
        {
            var expressionResolution = new ExpressionResolution();

            expressionResolution.TableAlias = GetTableAlias(((RelmTable)memberExpression.Expression.Type.GetCustomAttributes(typeof(RelmTable), true).FirstOrDefault())?.TableName);

            expressionResolution.FieldName = memberExpression.Member.Name;
            expressionResolution.ParameterName = GenerateParameterName(expressionResolution);

            if (string.IsNullOrWhiteSpace(expressionResolution.TableAlias))
                throw new TypeAccessException($"Could not find 'RelmTable' custom attribute on type: [{memberExpression.Expression.Type.FullName}]");

            //return new Tuple<string, string, string>(fieldName, parameterName, currentAlias);
            return expressionResolution;
        }

        private string GenerateParameterName(ExpressionResolution expressionResolution)
        {
            var duplicateCount = 0;
            var parameterName = $"@_{expressionResolution.FieldName}_";

            QueryParameters = QueryParameters ?? new Dictionary<string, object>();
            while (QueryParameters.ContainsKey($"{parameterName}{++duplicateCount}_")) ;

            parameterName = $"{parameterName}{duplicateCount}_";

            if (QueryParameters.ContainsKey(parameterName))
                throw new AccessViolationException($"Key {parameterName} already exists.");

            return parameterName;
        }

        private string GetTableAlias(string PropertyName)
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
                return null;

            if (_usedTableAliases.ContainsKey(PropertyName))
                return _usedTableAliases[PropertyName];

            var aliasCount = _usedTableAliases.Count;
            var currentAlias = string.Concat(Enumerable.Repeat(((char)((aliasCount % 26) + 97)).ToString(), (int)(aliasCount / 26.0) + 1));

            _usedTableAliases.Add(_underscoreProperties[PropertyName], currentAlias);

            return string.Empty;
        }

        private object ResolveParameter(Expression resolvableExpression, string tableAlias, string parameterName, bool asStringValue = false)
        {
            var parameterValue = ExpressionUtilities.GetValue(resolvableExpression);

            if (asStringValue)
                parameterValue = parameterValue.ToString();

            QueryParameters = QueryParameters ?? new Dictionary<string, object>();
            if (!QueryParameters.ContainsKey(parameterName))
                QueryParameters.Add(parameterName, null);

            QueryParameters[parameterName] = resolvableExpression.Type == typeof(bool)
                ? ((bool)parameterValue ? 1 : 0)
                : parameterValue;

            return parameterValue;
        }
    }
}
