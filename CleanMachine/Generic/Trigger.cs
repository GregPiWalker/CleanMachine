using log4net;
using System;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reflection;

namespace CleanMachine.Generic
{
    //TODO: Consider making this IDisposable

    /// <summary>
    /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TEventArgs"></typeparam>
    public class Trigger<TSource, TEventArgs> : TriggerBase where TSource : class //where TEventArgs : EventArgs
    {
        private delegate void EventHandlerDelegate(object sender, TEventArgs args);
        private EventHandlerDelegate _handler;
        private EventInfo _eventInfo;
        private string _filterName;

        /// <summary>
        /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="filter"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public Trigger(TSource source, string eventName, Constraint<TEventArgs> filter, IScheduler scheduler, Logger logger)
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
        public Trigger(TSource source, string eventName, IScheduler scheduler, Logger logger)
            : this(source, eventName, null, scheduler, logger)
        {
        }

        /// <summary>
        /// Creates a trigger that responds to an event of type <see cref="EventHandler{TEventArgs}"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventName"></param>
        /// <param name="logger"></param>
        public Trigger(TSource source, string eventName, Logger logger)
            : this(source, eventName, null, null, logger)
        {
        }

        protected virtual Type EventSourceType => typeof(TSource);

        protected void Initialize(string eventName, Constraint<TEventArgs> filter)
        {
            _handler = HandleEventRaised;
            FindEventInfo(eventName);
            Filter = filter;
            _filterName = filter == null ? string.Empty : filter.Name;
        }

        protected void FindEventInfo(string eventName)
        {
            _eventInfo = EventSourceType.GetEvent(eventName, FullAccessBindingFlags);
            if (_eventInfo == null)
            {
                // First try to get events from inherited interfaces.  This must be done
                // explicitly when the source is an interface because its Type class does 
                // not return events that are inherited from other interfaces.
                foreach (var interfaceT in EventSourceType.GetInterfaces())
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
                throw new ArgumentException($"No event named {eventName} was found on the {EventSourceType.Name} type.");
            }

            if (_eventInfo.EventHandlerType != GetExpectedType())
            {
                throw new ArgumentException($"{eventName} has the wrong delegate type {_eventInfo.EventHandlerType.Name}.  Expected {GetExpectedType().Name}.");
            }

            Name = $"{EventSourceType.Name}.{eventName}<{GetExpectedType().Name}>";
        }

        /// <summary>
        /// Gets the shadow implementation of the base Filter in order to provide a conditional parameter.
        /// </summary>
        public Constraint<TEventArgs> Filter { get; protected set; }

        protected bool IsAttachedToSourceEvent { get; private set; }

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
            if (Source == null)
            {
                _logger.Error($"Trigger '{Name}' failed to resolve a source object.  The trigger will not function.");
                return;
            }

            AttachToSourceEvent(Source);
        }

        protected override void Disable()
        {
            if (Source == null)
            {
                return;
            }

            DetachFromSourceEvent(Source);
        }

        protected void AttachToSourceEvent(object evSource)
        {
            if (IsAttachedToSourceEvent)
            {
                return;
            }

            var target = Delegate.CreateDelegate(_eventInfo.EventHandlerType, _handler.Target, _handler.Method);

            // Cannot use _eventInfo.AddEventHandler(Source, target) to access private members.
            MethodInfo addMethodInfo = _eventInfo.GetAddMethod(true);
            addMethodInfo.Invoke(evSource, FullAccessBindingFlags, null, new object[] { target }, CultureInfo.CurrentCulture);
            IsAttachedToSourceEvent = true;

            _logger.Trace($"{GetType().Name} '{Name}': Is attached.");
        }

        protected void DetachFromSourceEvent(object evSource)
        {
            if (!IsAttachedToSourceEvent)
            {
                return;
            }

            var target = Delegate.CreateDelegate(_eventInfo.EventHandlerType, _handler.Target, _handler.Method);

            // Cannot use _eventInfo.RemoveEventHandler(Source, target) to access private members.
            MethodInfo removeMethodInfo = _eventInfo.GetRemoveMethod(true);
            removeMethodInfo.Invoke(evSource, FullAccessBindingFlags, null, new object[] { target }, CultureInfo.CurrentCulture);
            IsAttachedToSourceEvent = false;

            _logger.Trace($"{GetType().Name} '{Name}': Is detached.");
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
