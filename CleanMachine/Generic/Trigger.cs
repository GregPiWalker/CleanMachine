﻿using log4net;
using System;
using System.Globalization;
using System.Reflection;

namespace CleanMachine.Generic
{
    /// <summary>
    /// A trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
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
            _eventInfo = typeof(TSource).GetEvent(eventName, FullAccessBindingFlags);
            if (_eventInfo == null)
            {
                throw new ArgumentException($"No event named {eventName} was found on the {typeof(TSource)} type.");
            }

            if (_eventInfo.EventHandlerType != GetExpectedType())
            {
                throw new ArgumentException($"{eventName} has the wrong delegate type {_eventInfo.EventHandlerType.Name}.  Expected {GetExpectedType().Name}.");
            }

            Name = $"{typeof(TSource).Name}.{eventName}<{GetExpectedType().Name}>";
            Filter = filter;
            _filterName = filter == null ? string.Empty : filter.Name;
        }

        public Trigger(TSource source, string eventName, ILog logger)
            : this(source, eventName, null, logger)
        {
        }

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
            var target = Delegate.CreateDelegate(_eventInfo.EventHandlerType, _handler.Target, _handler.Method);

            // Cannot use _eventInfo.AddEventHandler(Source, target) to access private members.
            MethodInfo addMethodInfo = _eventInfo.GetAddMethod(true);
            addMethodInfo.Invoke(Source, FullAccessBindingFlags, null, new object[] { target }, CultureInfo.CurrentCulture);
        }

        protected override void Disable()
        {
            var target = Delegate.CreateDelegate(_eventInfo.EventHandlerType, _handler.Target, _handler.Method);

            // Cannot use _eventInfo.RemoveEventHandler(Source, target) to access private members.
            MethodInfo removeMethodInfo = _eventInfo.GetRemoveMethod(true);
            removeMethodInfo.Invoke(Source, FullAccessBindingFlags, null, new object[] { target }, CultureInfo.CurrentCulture);
        }

        protected virtual Type GetExpectedType()
        {
            return typeof(EventHandler<TEventArgs>);
        }

        private void HandleEventRaised(object sender, TEventArgs args)
        {
            Trip(sender, args);
        }
    }
}
