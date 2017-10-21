using System;
using System.Linq.Expressions;

namespace RemoteHost
{
    public interface IRemostHosted<THosted> : IDisposable
        where THosted : class, new()
    {
        void Start();

        /// <summary>
        /// Call a method in THosted class that has a return type.
        /// </summary>
        /// <typeparam name="TResult">The return type</typeparam>
        /// <param name="methodCall">Expression of a single function call of a method in THosted class</param>
        /// <returns>The value of the method call</returns>
        TResult Call<TResult>(Expression<Func<THosted, TResult>> methodCall);

        /// <summary>
        /// Call a mthod in THosted class that is a void return type.
        /// </summary>
        /// <param name="methodCall">Expression of a single function call of a method in THosted class</param>
        //void Call(Expression<Action<THosted>> methodCall);

        //void Set<TParameter>(Expression<Func<THosted, TParameter>> setCall, TParameter value);
        //TResult Get<TResult>(Expression<Func<THosted,TResult>> getCall);
    }
}