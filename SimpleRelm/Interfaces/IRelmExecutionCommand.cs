using SimpleRelm.Models;
using SimpleRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static SimpleRelm.Enums.Commands;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.Interfaces
{
    public interface IRelmExecutionCommand
    {
        Command InitialCommand { get; }
        Expression InitialExpression { get; }
        int AdditionalCommandCount { get; }

        RelmExecutionCommand AddAdditionalCommand(Command command, Expression expression);
        List<RelmExecutionCommand> GetAdditionalCommands();
        ForeignKeyNavigationOptions GetForeignKeyNavigationOptions<T>(ICollection<T> _items);
    }
}
