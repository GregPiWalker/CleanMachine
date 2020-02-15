using log4net;
using System;
using System.Reflection;

namespace CleanMachine.Generic
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    public class Trigger<TSource, TEventArgs> : TriggerBase where TEventArgs : EventArgs
    {
        private delegate void EventHandlerDelegate(object sender, TEventArgs args);
        private readonly EventHandlerDelegate _handler;
        private readonly EventInfo _eventInfo;
        private readonly string _filterName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="filter"></param>
        public Trigger(TSource source, string eventName, Constraint<TEventArgs> filter, ILog logger)
            : base(string.Empty, source, logger)
        {
            _handler = HandleEventRaised;
            _eventInfo = typeof(TSource).GetEvent(eventName);
            if (_eventInfo == null)
            {
                throw new ArgumentException($"No event named {eventName} was found on the {typeof(TSource)} type.");
            }

            if (_eventInfo.EventHandlerType != typeof(EventHandler<TEventArgs>))
            {
                throw new ArgumentException($"{eventName} has the wrong delegate type {_eventInfo.EventHandlerType.Name}.  Expected {typeof(EventHandler<TEventArgs>).Name}.");
            }

            Name = $"{typeof(TSource).Name}.{eventName}<{typeof(TEventArgs).Name}>";
            Filter = filter;
            _filterName = filter == null ? string.Empty : filter.Name;
        }

        public Trigger(TSource source, string eventName, ILog logger)
            : this(source, eventName, null, logger)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override Type SourceType => typeof(TSource);

        /// <summary>
        /// Gets the shadow implementation of the base Filter in order to provide a conditional parameter.
        /// </summary>
        public Constraint<TEventArgs> Filter { get; protected set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(_filterName) ? Name : $"{Name}[{_filterName}]";
        }

        public override bool CanTrigger(EventArgs causeEventArgs)
        {
            return CanTrigger(causeEventArgs as TEventArgs);
        }

        public bool CanTrigger(TEventArgs causeEventArgs)
        {
            return Filter == null || Filter.IsTrue(causeEventArgs);
        }

        protected override void Enable()
        {
            _eventInfo.AddEventHandler(Source, Delegate.CreateDelegate(_eventInfo.EventHandlerType, _handler.Target, _handler.Method));
        }

        protected override void Disable()
        {
            _eventInfo.RemoveEventHandler(Source, Delegate.CreateDelegate(_eventInfo.EventHandlerType, _handler.Target, _handler.Method));
        }

        private void HandleEventRaised(object sender, TEventArgs args)
        {
            Trip(sender, args);
        }
    }
}
