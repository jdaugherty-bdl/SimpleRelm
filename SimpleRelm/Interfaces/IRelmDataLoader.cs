using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static SimpleRelm.RelmInternal.Helpers.Operations.ExpressionEvaluator;

namespace SimpleRelm.Interfaces
{
    public interface IRelmDataLoader<T>
    {
        Dictionary<Command, List<Expression>> LastCommandsExecuted { get; set; }

        ICollection<T> GetLoadData();
        int WriteData();
        void AddExpression(Command command, Expression expression);
        void AddSingleExpression(Command command, Expression expression);
        bool HasUnderscoreProperty(string PropertyKey);
    }
}
