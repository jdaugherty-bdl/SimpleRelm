using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using SimpleRelm.Options;
using SimpleRelm.RelmInternal.Helpers.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.Models
{
    public class RelmDefaultDataLoader<T> : IRelmDataLoader<T> where T : IRelmModel, new()
    {
        public Dictionary<Command, List<Expression>> LastCommandsExecuted { get; set; }

        // this is internal to facilitate unit testing only
        internal virtual string _tableName => typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName;

        private readonly RelmContextOptionsBuilder _contextOptionsBuilder;

        private string _fullPropertySelectList;
        private Dictionary<string, string> _underscoreProperties;

        private Dictionary<Command, List<Expression>> _commands;

        public RelmDefaultDataLoader()
        {
            InitialSetup();
        }

        public RelmDefaultDataLoader(RelmContextOptionsBuilder contextOptionsBuilder)
        {
            this._contextOptionsBuilder = contextOptionsBuilder;
         
            InitialSetup();
        }

        private void InitialSetup()
        { 
            // get the table name from the DALTable attribute of T
            //_tableName = typeof(T).GetCustomAttribute<RelmTable>(false)?.TableName;

            if (string.IsNullOrWhiteSpace(_tableName))
                throw new Exception($"RelmTable attribute not found on type {typeof(T).Name}");

            // get a list of all properties on T that are marked with the DALResolvable attribute
            _underscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true).ToDictionary(x => x.Value.Item1, x => x.Key);

            // get a list of all class property names surrounded by ` quotes separated by commas
            _fullPropertySelectList = string.Join(", ", _underscoreProperties.Select(p => $"a.`{p.Value}`"));
        }


        public bool HasUnderscoreProperty(string PropertyKey) => _underscoreProperties?.ContainsKey(PropertyKey) ?? false;

        public void AddExpression(Command command, Expression expression)
        {
            PrewarmQuery(command).Add(expression);
        }

        public void AddSingleExpression(Command command, Expression expression)
        {
            var expressions = PrewarmQuery(command);

            if (expressions.Count == 0)
                expressions.Add(null);

            expressions[0] = expression;
        }

        private List<Expression> PrewarmQuery(Command PredicateCommand)
        {
            if (_commands == null)
                _commands = new Dictionary<Command, List<Expression>>();

            if (!_commands.ContainsKey(PredicateCommand))
                _commands.Add(PredicateCommand, new List<Expression>());

            return _commands[PredicateCommand];
        }

        public virtual ICollection<T> GetLoadData()
        {
            var findOptions = new Dictionary<string, object>();
            var selectQuery = GetSelectQuery(findOptions);

            return PullData(selectQuery, findOptions);
        }

        public virtual ICollection<T> PullData(string selectQuery, Dictionary<string, object> findOptions)
        {
            if (_contextOptionsBuilder.OptionsBuilderType == RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return RelmHelper.GetDataObjects<T>(_contextOptionsBuilder.DatabaseConnection, selectQuery, findOptions, SqlTransaction: _contextOptionsBuilder.DatabaseTransaction).ToList();
            else
                return RelmHelper.GetDataObjects<T>(_contextOptionsBuilder.ConnectionStringType, selectQuery, findOptions).ToList();
        }

        public int WriteData()
        {
            var findOptions = new Dictionary<string, object>();

            var selectQuery = GetUpdateQuery(findOptions);

            if (_contextOptionsBuilder.OptionsBuilderType == RelmContextOptionsBuilder.OptionsBuilderTypes.OpenConnection)
                return RelmHelper.DoDatabaseWork<int>(_contextOptionsBuilder.DatabaseConnection, selectQuery, findOptions, SqlTransaction: _contextOptionsBuilder.DatabaseTransaction);
            else
                return RelmHelper.DoDatabaseWork<int>(_contextOptionsBuilder.ConnectionStringType, selectQuery, findOptions);
        }

        internal string GetSelectQuery(Dictionary<string, object> FindOptions)
        {
            return BuildQuery($"SELECT {_fullPropertySelectList} ", FindOptions, true);
        }

        internal string GetUpdateQuery(Dictionary<string, object> FindOptions)
        {
            return BuildQuery($"UPDATE ", FindOptions, false);
        }

        private string BuildQuery(string QueryPredicate, Dictionary<string, object> FindOptions, bool isSelect)
        {
            // hardcode first table alias to 'a', and inject that into the expression evaluator
            var expressionEvaluator = new ExpressionEvaluator(_tableName, _underscoreProperties, UsedTableAliases: new Dictionary<string, string> { [_tableName] = "a" });

            // evaluate all the pieces of the query
            var queryPieces = new Dictionary<Command, List<string>>();
            if (_commands != null)
            {
                foreach (var command in _commands)
                {
                    if (!queryPieces.ContainsKey(command.Key))
                        queryPieces.Add(command.Key, new List<string>());

                    // evaluate all expressions, except references and collections as those are evaluated after selection
                    switch (command.Key)
                    {
                        case Command.Where:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateWhere(command, FindOptions));
                            break;
                        case Command.OrderBy:
                        case Command.OrderByDescending:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateOrderBy(command, command.Key == Command.OrderByDescending));
                            break;
                        case Command.Set:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateSet(command, FindOptions));
                            break;
                        case Command.Limit:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateLimit(command));
                            break;
                        case Command.GroupBy:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateGroupBy(command, FindOptions));
                            break;
                        case Command.DistinctBy:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateDistinctBy(command));
                            break;
                        case Command.Count:
                            queryPieces[command.Key].Add(expressionEvaluator.EvaluateCount(command));
                            break;
                    }
                }
            }

            // build the query
            var predicatePieces = QueryPredicate.Split(' ');
            var findQuery = predicatePieces[0];

            findQuery += " ";

            if (queryPieces.ContainsKey(Command.Count))
            {
                findQuery += queryPieces[Command.Count];
            }
            else
            {
                if (queryPieces.ContainsKey(Command.DistinctBy))
                {
                    findQuery += string.Join("\n", queryPieces[Command.DistinctBy]);
                    findQuery += ", ";
                }

                if (predicatePieces.Length > 1)
                    findQuery += string.Join(" ", predicatePieces.Skip(1));
            }

            if (isSelect)
                findQuery += " FROM ";
            findQuery += $" `{_tableName}` a "; // hardcode first table alias to 'a'

            if (queryPieces.ContainsKey(Command.Reference))
                findQuery += string.Join("\n", queryPieces[Command.Reference]);
            if (queryPieces.ContainsKey(Command.Collection))
                findQuery += string.Join("\n", queryPieces[Command.Collection]);

            if (queryPieces.ContainsKey(Command.Where))
                findQuery += string.Join("\n", queryPieces[Command.Where]);

            if (queryPieces.ContainsKey(Command.Set))
                findQuery += string.Join("\n", queryPieces[Command.Set]);

            if (queryPieces.ContainsKey(Command.OrderBy))
                findQuery += string.Join("\n", queryPieces[Command.OrderBy]);
            if (queryPieces.ContainsKey(Command.OrderByDescending))
                findQuery += string.Join("\n", queryPieces[Command.OrderByDescending]);
            if (queryPieces.ContainsKey(Command.GroupBy))
                findQuery += string.Join("\n", queryPieces[Command.GroupBy]);

            if (queryPieces.ContainsKey(Command.Limit))
                findQuery += string.Join("\n", queryPieces[Command.Limit]);

            LastCommandsExecuted = _commands;
            _commands = null;

            findQuery += ";";

            return findQuery;
        }
    }
}
