using log4net;
using System;

namespace CleanMachine.Generic
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    public class DelegateTrigger<TSource, TDelegate, TEventArgs> : Trigger<TSource, TEventArgs> where TEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="filter"></param>
        public DelegateTrigger(TSource source, string eventName, Constraint<TEventArgs> filter, ILog logger)
            : base(source, eventName, filter, logger)
        {
        }

        public DelegateTrigger(TSource source, string eventName, ILog logger)
            : this(source, eventName, null, logger)
        {
        }

        protected override Type GetExpectedType()
        {
            return typeof(TDelegate);
        }
    }
}
