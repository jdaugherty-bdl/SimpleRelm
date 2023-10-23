using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Interfaces
{
    public interface IRelmDataSet<T> : ICollection<T>, IRelmDataSetBase where T : IRelmModel, new()
    {
        IRelmFieldLoader SetFieldLoader(string fieldName, IRelmFieldLoader dataLoader);
        IRelmDataLoader<T> SetDataLoader(IRelmDataLoader<T> dataLoader);
        IRelmDataSet<T> Where(Expression<Func<T, bool>> predicate);
        IRelmDataSet<T> Reference(Expression<Func<T, object>> predicate);
        IRelmDataSet<T> Collection(Expression<Func<T, object>> predicate);
        T Find(int ItemId);
        T Find(string ItemInternalId);
        T FirstOrDefault();
        T FirstOrDefault(bool LoadItems);
        T FirstOrDefault(Expression<Func<T, bool>> predicate);
        T FirstOrDefault(Expression<Func<T, bool>> predicate, bool LoadItems);
        ICollection<T> Load();
        int Write();
        IRelmDataSet<T> Entry(T Item);
        IRelmDataSet<T> Entry(T Item, bool Persist = true);
        IRelmDataSet<T> OrderBy(Expression<Func<T, object>> predicate);
        IRelmDataSet<T> OrderByDescending(Expression<Func<T, object>> predicate);
        IRelmDataSet<T> Set(Expression<Func<T, T>> predicate);
        IRelmDataSet<T> GroupBy(Expression<Func<T, object>> predicate);
        IRelmDataSet<T> Limit(int LimitCount);
        IRelmDataSet<T> DistinctBy(Expression<Func<T, object>> predicate);
        int Save(T Item);
        int Save();
        T New();
        T New(dynamic NewObjectParameters, bool Persist = true);
        new int Add(T item);
        int Add(T item, bool Persist);
        int Add(ICollection<T> items);
        int Add(ICollection<T> items, bool Persist);
    }
}
