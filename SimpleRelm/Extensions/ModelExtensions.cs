using SimpleRelm.Interfaces;
using SimpleRelm.Models;
using SimpleRelm.Options;
using SimpleRelm.RelmInternal.Helpers.DataTransfer;
using SimpleRelm.RelmInternal.Helpers.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Extensions
{
    public static class ModelExtensions
    {
        /*
        public IRelmModel LoadForeignKeyField<S>(RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<IRelmModel, S>> predicate)
        {
            /*
            //public T LoadForeignKeyField<T, S>(RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new()
            return new ForeignKeyLoader<T>(this, relmContextOptionsBuilder)
                .LoadForeignKey(predicate)
                ?.FirstOrDefault();
            * /
            var loaderType = typeof(ForeignKeyLoader<>).MakeGenericType(this.GetType());
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { this, relmContextOptionsBuilder });
            var loaderResult = loaderType.GetMethod(nameof(ForeignKeyLoader<RelmModel>.LoadForeignKey)).Invoke(loaderInstance, new object[] { predicate });

            return ((ICollection<IRelmModel>)loaderResult).FirstOrDefault();
        }
        */
        public static T LoadForeignKeyField<T, S>(this T inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(ForeignKeyLoader<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { inputModel, relmContextOptionsBuilder });

            var loaderMethodGeneric = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey) 
                    && x.GetParameters().Length == 1 
                    && x.GetParameters()
                        .Any(y => y.ParameterType
                            .GetGenericArguments()
                            .Any(z => z.GetGenericArguments()
                                .All(aa => !aa.IsGenericType))))
                .FirstOrDefault();

            var loaderMethod = loaderMethodGeneric.MakeGenericMethod(typeof(S));

            var loaderResult = loaderMethod.Invoke(loaderInstance, new object[] { predicate });

            return ((ICollection<T>)loaderResult).FirstOrDefault();
        }

        public static T LoadForeignKeyField<T, S, R>(this T inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate, IRelmDataLoader<R> customDataLoader) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(ForeignKeyLoader<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { inputModel, relmContextOptionsBuilder });

            var loaderMethodGeneric = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey)
                    && x.GetParameters().Length == 2
                    && x.GetParameters()
                        .Any(y => y.ParameterType.IsGenericType && y.ParameterType.GetGenericTypeDefinition() == typeof(IRelmDataLoader<>)))
                .Where(x => x.GetParameters()
                    .All(y => y.ParameterType.GetGenericTypeDefinition() == typeof(IRelmDataLoader<>)
                        || y.ParameterType
                            .GetGenericArguments()
                            .Where(z => z.GetGenericTypeDefinition() == typeof(Func<,>))
                            .Any(z => z.GetGenericArguments().All(aa => !aa.IsGenericType))))
                .FirstOrDefault();

            var loaderMethod = loaderMethodGeneric.MakeGenericMethod(typeof(S), typeof(R));

            var loaderResult = loaderMethod.Invoke(loaderInstance, new object[] { predicate, customDataLoader });

            return ((ICollection<T>)loaderResult).FirstOrDefault();
        }

        public static T LoadForeignKeyField<T, S, R>(this T inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<S>>> predicate, IRelmDataLoader<R> customDataLoader) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(ForeignKeyLoader<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { inputModel, relmContextOptionsBuilder });

            var loaderMethodGeneric = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey)
                    && x.GetParameters().Length == 2
                    && x.GetParameters()
                        .Any(y => y.ParameterType.IsGenericType && y.ParameterType.GetGenericTypeDefinition() == typeof(IRelmDataLoader<>)))
                .Where(x => x.GetParameters()
                    .All(y => y.ParameterType.GetGenericTypeDefinition() == typeof(IRelmDataLoader<>)
                        || y.ParameterType
                            .GetGenericArguments()
                            .Where(z => z.GetGenericTypeDefinition() == typeof(Func<,>))
                            .Any(z => z.GetGenericArguments().Any(aa => aa.IsGenericType && aa.GetGenericTypeDefinition() == typeof(ICollection<>)))))
                .FirstOrDefault();

            var loaderMethod = loaderMethodGeneric.MakeGenericMethod(typeof(S), typeof(R));

            var loaderResult = loaderMethod.Invoke(loaderInstance, new object[] { predicate, customDataLoader });

            return ((ICollection<T>)loaderResult).FirstOrDefault();
        }

        /*
        public static T LoadForeignKeyField<T, S, R>(this T inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, ICollection<S>>> predicate, IRelmDataLoader<R> customDataLoader) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(ForeignKeyLoader<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { inputModel, relmContextOptionsBuilder });

            var ddd = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey))
                .Select(x => x.GetParameters()
                        .Select(y => y.ParameterType
                            .GetGenericArguments()
                            .Select(z => z.GetGenericArguments()
                                //.Select(aa => !aa.IsGenericType)
                                .ToList())
                            .ToList())
                        .ToList())
                .ToList();

            var eee = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey)
                    && x.GetParameters().Length == 2
                    && x.GetParameters()
                        .Any(y => y.ParameterType.IsGenericType && y.ParameterType.GetGenericTypeDefinition() == typeof(IRelmDataLoader<>)))
                .Select(x => x.GetParameters()
                    .Select(y => y.ParameterType
                        .GetGenericArguments()
                        //.Select(z => z.DeclaringType?.GetGenericTypeDefinition() ?? z.DeclaringType)
                        //.ToList()
                        )
                    .ToList())
                .ToList();
            var eee = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey)
                    && x.GetParameters().Length == 2
                    && x.GetParameters()
                        .Any(y => y.ParameterType.IsGenericType && y.ParameterType.GetGenericTypeDefinition() == typeof(IRelmDataLoader<>)))
                .Select(x => x.GetParameters()
                    .Select(y => y.ParameterType.GetGenericTypeDefinition() == typeof(IRelmDataLoader<>)
                        ? new Type[] { y.ParameterType }
                        : (y.ParameterType.GetGenericArguments().Where(z => z.GetGenericTypeDefinition() == typeof(Func<,>)).Any(z => z.GetGenericArguments().Any(aa => aa.IsGenericType && aa.GetGenericTypeDefinition() == typeof(ICollection<>)))
                            ? y.ParameterType.GetGenericArguments().FirstOrDefault().GetGenericArguments()
                            : default)
                        //.Select(z => z.DeclaringType?.GetGenericTypeDefinition() ?? z.DeclaringType)
                        //.ToList()
                        )
                    .ToList())
                .ToList();

            var loaderMethodGeneric = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey) 
                    && x.GetParameters().Length == 1 
                    && x.GetParameters()
                        .Any(y => y.ParameterType
                            .GetGenericArguments()
                            .Any(z => z.GetGenericArguments()
                                .All(aa => !aa.IsGenericType))))
                .FirstOrDefault();

            var loaderMethod = loaderMethodGeneric.MakeGenericMethod(typeof(S));

            var loaderResult = loaderMethod.Invoke(loaderInstance, new object[] { predicate });

            return ((ICollection<T>)loaderResult).FirstOrDefault();
        }
        */

        public static ICollection<T> LoadForeignKeyField<T, S>(this ICollection<T> inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(ForeignKeyLoader<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { inputModel, relmContextOptionsBuilder });

            var loaderMethod = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(ForeignKeyLoader<T>.LoadForeignKey) 
                    && x.GetParameters().Length == 1 
                    && x.GetParameters()
                        .Any(y => y.ParameterType
                            .GetGenericArguments()
                            .Any(z => z.GetGenericArguments()
                                .Any(aa => aa.IsGenericType && aa.GetGenericTypeDefinition() == typeof(ICollection<>)))))
                .FirstOrDefault();

            var loaderResult = loaderMethod.Invoke(loaderInstance, new object[] { predicate });

            return (ICollection<T>)loaderResult;
        }

        /*
        public IRelmModel LoadDataLoaderField<S>(Expression<Func<IRelmModel, S>> predicate)
        {
            /*
            //public T LoadDataLoaderField<T, S>(Expression<Func<T, S>> predicate) where T : IRelmModel, new()
            return new DataLoaderHelper<T>(this)
                .LoadField(predicate)
                ?.FirstOrDefault();
            * /
            var loaderType = typeof(DataLoaderHelper<>).MakeGenericType(this.GetType());
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { this });
            var loaderResult = loaderType.GetMethod(nameof(DataLoaderHelper<RelmModel>.LoadField)).Invoke(loaderInstance, new object[] { predicate });

            return ((ICollection<IRelmModel>)loaderResult).FirstOrDefault();
        }
        */
        public static T LoadDataLoaderField<T, S>(this T inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(DataLoaderHelper<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { inputModel, relmContextOptionsBuilder });

            var loaderMethod = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(DataLoaderHelper<T>.LoadField) 
                    && x.GetParameters().Length == 1 
                    && x.GetParameters()
                        .Any(y => y.ParameterType
                            .GetGenericArguments()
                            .Any(z => z.GetGenericArguments()
                                .All(aa => !aa.IsGenericType))))
                .FirstOrDefault();

            var loaderResult = loaderMethod.Invoke(loaderInstance, new object[] { predicate });

            return ((ICollection<T>)loaderResult).FirstOrDefault();
        }

        public static ICollection<T> LoadDataLoaderField<T, S>(this ICollection<T> inputModel, RelmContextOptionsBuilder relmContextOptionsBuilder, Expression<Func<T, S>> predicate) where T : IRelmModel, new() where S : IRelmModel, new()
        {
            var loaderType = typeof(DataLoaderHelper<>).MakeGenericType(typeof(T));
            var loaderInstance = Activator.CreateInstance(loaderType, new object[] { inputModel, relmContextOptionsBuilder });

            var loaderMethod = loaderType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name == nameof(DataLoaderHelper<T>.LoadField) 
                    && x.GetParameters().Length == 1 
                    && x.GetParameters()
                        .Any(y => y.ParameterType
                            .GetGenericArguments()
                            .Any(z => z.GetGenericArguments()
                                .Any(aa => aa.IsGenericType && aa.GetGenericTypeDefinition() == typeof(ICollection<>)))))
                .FirstOrDefault();

            var loaderResult = loaderMethod.Invoke(loaderInstance, new object[] { predicate });

            return (ICollection<T>)loaderResult;
        }
    }
}
