using log4net;
using System;
using System.Reactive.Concurrency;

namespace CleanMachine.Generic
{
    /// <summary>
    /// An event trigger that uses a Delegate type definition instead of an <see cref="EventHandler{TEventArgs}"/>.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    public class DelegateTrigger<TSource, TDelegate, TEventArgs> : Trigger<TSource, TEventArgs> //where TEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="filter"></param>
        /// <param name="tripScheduler"></param>
        /// <param name="logger"></param>
        public DelegateTrigger(TSource source, string eventName, Constraint<TEventArgs> filter, IScheduler tripScheduler, Logger logger)
            : base(source, eventName, filter, tripScheduler, logger)
        {
        }

        public DelegateTrigger(TSource source, string eventName, IScheduler tripScheduler, Logger logger)
            : this(source, eventName, null, tripScheduler, logger)
        {
        }

        protected override Type GetExpectedType()
        {
            return typeof(TDelegate);
        }
    }
}
