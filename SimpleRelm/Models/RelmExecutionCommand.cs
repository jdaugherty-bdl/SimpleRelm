using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.Models
{
    public class RelmExecutionCommand : IRelmExecutionCommand
    {
        public Command InitialCommand { get; private set; }
        public Expression InitialExpression { get; private set; }
        public int AdditionalCommandCount => _additionalCommands?.Count ?? 0;

        private readonly List<RelmExecutionCommand> _additionalCommands = new List<RelmExecutionCommand>();

        public RelmExecutionCommand(Command command, Expression expression)
        {
            InitialCommand = command;
            InitialExpression = expression;
        }

        public RelmExecutionCommand AddAdditionalCommand(Command command, Expression expression)
        {
            _additionalCommands.Add(new RelmExecutionCommand(command, expression));

            return this;
        }

        public List<RelmExecutionCommand> GetAdditionalCommands()
        {
            return _additionalCommands;
        }
    }
}
