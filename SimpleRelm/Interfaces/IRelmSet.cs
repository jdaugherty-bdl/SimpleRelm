using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces
{
    public interface IRelmSet<T> : ICollection<T>, IRelmSetBase where T : IRelmModel, new()
    {
        IRelmSet<T> Where(Expression<Func<T, bool>> predicate);
        IRelmSet<T> Reference(Expression<Func<T, object>> predicate);
        IRelmSet<T> Collection(Expression<Func<T, object>> predicate);
        T Find(int ItemId);
        T Find(string ItemInternalId);
        T FirstOrDefault();
        T FirstOrDefault(bool LoadItems);
        T FirstOrDefault(Expression<Func<T, bool>> predicate);
        T FirstOrDefault(Expression<Func<T, bool>> predicate, bool LoadItems);
        ICollection<T> Load();
        int Write();
        IRelmSet<T> Entry(T Item);
        IRelmSet<T> OrderBy(Expression<Func<T, object>> predicate);
        IRelmSet<T> OrderByDescending(Expression<Func<T, object>> predicate);
        IRelmSet<T> Set(Expression<Func<T, T>> predicate);
        IRelmSet<T> GroupBy(Expression<Func<T, object>> predicate);
        IRelmSet<T> Limit(int LimitCount);
        IRelmSet<T> DistinctBy(Expression<Func<T, object>> predicate);
        T Save(T Item);
        void Save();
        T New();
        T New(dynamic NewObjectParameters, bool Persist = true);
    }
}
