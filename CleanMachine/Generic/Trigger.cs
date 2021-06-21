using log4net;
using System;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reflection;

namespace CleanMachine.Generic
{
    /// <summary>
    /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    public class Trigger<TSource, TEventArgs> : TriggerBase //where TEventArgs : EventArgs
    {
        private delegate void EventHandlerDelegate(object sender, TEventArgs args);
        private EventHandlerDelegate _handler;
        private EventInfo _eventInfo;
        private string _filterName;
        private readonly Func<TSource> _lazySourceProvider;

        /// <summary>
        /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="filter"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public Trigger(TSource source, string eventName, Constraint<TEventArgs> filter, IScheduler scheduler, ILog logger)
            : base(string.Empty, source, scheduler, logger)
        {
            Initialize(eventName, filter);
        }

        /// <summary>
        /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public Trigger(TSource source, string eventName, IScheduler scheduler, ILog logger)
            : this(source, eventName, null, scheduler, logger)
        {
        }

        /// <summary>
        /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="logger"></param>
        public Trigger(TSource source, string eventName, ILog logger)
            : this(source, eventName, null, null, logger)
        {
        }

        /// <summary>
        /// Creates a lazily-bound trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// The lazy source will be harvested when this Trigger is enabled.
        /// </summary>
        /// <param name="lazySource"></param>
        /// <param name="eventName"></param>
        /// <param name="filter"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public Trigger(Func<TSource> lazySource, string eventName, Constraint<TEventArgs> filter, IScheduler scheduler, ILog logger)
            : base(string.Empty, null, scheduler, logger)
        {
            _lazySourceProvider = lazySource;
            Initialize(eventName, filter);
        }

        /// <summary>
        /// Creates a lazily-bound trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// The lazy source will be harvested when this Trigger is enabled.
        /// </summary>
        /// <param name="lazySource"></param>
        /// <param name="eventName"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public Trigger(Func<TSource> lazySource, string eventName, IScheduler scheduler, ILog logger)
            : this(lazySource, eventName, null, scheduler, logger)
        {
        }

        private void Initialize(string eventName, Constraint<TEventArgs> filter)
        {
            _handler = HandleEventRaised;
            _eventInfo = typeof(TSource).GetEvent(eventName, FullAccessBindingFlags);
            if (_eventInfo == null)
            {
                // First try to get events from inherited interfaces.  This must be done
                // explicitly when the source is an interface because its Type class does 
                // not return events that are inherited from other interfaces.
                foreach (var interfaceT in typeof(TSource).GetInterfaces())
                {
                    _eventInfo = interfaceT.GetEvent(eventName, FullAccessBindingFlags);
                    if (_eventInfo != null)
                    {
                        break;
                    }
                }
            }

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

        /// <summary>
        /// Gets the shadow implementation of the base Filter in order to provide a conditional parameter.
        /// </summary>
        public Constraint<TEventArgs> Filter { get; protected set; }

        public override bool IsSourceLazy => _lazySourceProvider != null;

        public override string ToString()
        {
            return string.IsNullOrEmpty(_filterName) ? Name : $"{Name}[{_filterName}]";
        }

        public override bool CanTrigger(/*EventArgs*/object causeEventArgs)
        {
            return CanTrigger((TEventArgs)causeEventArgs);
        }

        public bool CanTrigger(TEventArgs causeEventArgs)
        {
            if (!base.CanTrigger(causeEventArgs))
            {
                return false;
            }

            if (Filter != null && !Filter.IsTrue(causeEventArgs))
            {
                //log it
                return false;
            }

            return true;
        }

        protected override void Enable()
        {
            if (Source == null && _lazySourceProvider != null)
            {
                try
                {
                    Source = _lazySourceProvider();
                }
                catch (NullReferenceException ex)
                {
                    _logger?.Error($"Trigger '{Name}' failed to resolve a late-bound source object.  The trigger will not function.", ex);
                    return;
                }
            }

            var target = Delegate.CreateDelegate(_eventInfo.EventHandlerType, _handler.Target, _handler.Method);

            // Cannot use _eventInfo.AddEventHandler(Source, target) to access private members.
            MethodInfo addMethodInfo = _eventInfo.GetAddMethod(true);
            addMethodInfo.Invoke(Source, FullAccessBindingFlags, null, new object[] { target }, CultureInfo.CurrentCulture);
        }

        protected override void Disable()
        {
            if (Source == null)
            {
                return;
            }

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
