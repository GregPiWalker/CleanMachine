using System;
using System.Reflection;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using CleanMachine.Interfaces;
using log4net;

namespace CleanMachine
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class TriggerBase : ITrigger
    {
        public const BindingFlags FullAccessBindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        protected readonly IScheduler _tripScheduler;
        protected readonly ILog _logger;
        protected BooleanDisposable _visitorId;
        //TODO: looks like this should be the same object as the Machine's sync object, when no trigger scheduler is used.
        private readonly object _visitLock = new object();

        public TriggerBase(string name, object source, IScheduler tripScheduler, ILog logger)
        {
            _tripScheduler = tripScheduler;
            _logger = logger;
            Name = name;
            Source = source;
            IsActive = false;
        }

        public event EventHandler<TripEventArgs> Triggered;

        public string Name { get; protected set; }
        
        public object Source { get; protected set; }
        
        public bool VerboseLogging { get; set; }

        /// <summary>
        /// Indicates whether this trigger is connected and responding to surrounding events.
        /// </summary>
        public bool IsActive { get; protected internal set; }

        public virtual bool IsSourceLazy => false;

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Activate this trigger, making it responsive to surrounding events.
        /// </summary>
        /// <param name="stateVisitId"></param>
        public void Activate(IDisposable stateVisitId)
        {
            lock (_visitLock)
            {
                if (!IsActive)
                {
                    IsActive = true;
                    _visitorId = stateVisitId as BooleanDisposable;
                    Enable();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                Disable();
            }
        }

        /// <summary>
        /// Conditionally trip the trigger.  If tripped, the <see cref="Triggered"/> event is raised.
        /// If the Guard condition is false, then the trigger is not tripped.
        /// </summary>
        /// <param name="origin">The optional origin of the trip.</param>
        /// <param name="originEventArgs">The optional data related to the origin event.</param>
        public void Trip(object origin, object originEventArgs)
        {
            lock (_visitLock)
            {
                if (!IsActive)
                {
                    return;
                }

                if (_tripScheduler != null)
                {
                    // Making a local copy here prevents the lambda from potentially closing on the
                    // field after a new value has been assigned in the Activate method.
                    var localIdCopy = _visitorId;
                    _tripScheduler.Schedule(() => BeginTrip(origin, originEventArgs, localIdCopy));
                }
                else
                {
                    BeginTrip(origin, originEventArgs, _visitorId);
                }
            }
        }

        private void BeginTrip(object origin, object originEventArgs, IDisposable visitorId)
        {
            if (CanTrigger(originEventArgs))
            {
                // Add the trip source and its arguments to the route history.
                var trip = new TripEventArgs(visitorId, new DataWaypoint(origin, originEventArgs));
                OnTriggered(trip);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="causeEventArgs"></param>
        /// <returns></returns>
        public virtual bool CanTrigger(/*EventArgs*/object causeEventArgs)
        {
            // When Triggers are asynchronous, they are queued up and serviced serially.  While moving through the queue,
            // they can go stale for different reasons.
            if (_tripScheduler != null)
            {
                // Provide escape route in case the trigger was deactivated while the trip was waiting to be serviced.
                if (!IsActive)
                {
                    _logger.Debug($"{Name}.{nameof(OnTriggered)}:  trigger trip rejected for '{ToString()}'. Trigger is currently inactive.");
                    return false;
                }

                // Provide escape route in case the trip became irrelevant while it was waiting to be serviced.
                if (_visitorId != null && _visitorId.IsDisposed)
                {
                    _logger.Debug($"{Name}.{nameof(OnTriggered)}:  trigger trip rejected for  '{ToString()}'.  The trip is no longer valid in relation to its surroundings.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Connect this trigger to it's associated event.
        /// </summary>
        protected virtual void Enable()
        {
            // Empty base.
        }

        /// <summary>
        /// Disconnect this trigger from it's associated event.
        /// </summary>
        protected virtual void Disable()
        {
            // Empty base.
        }

        /// <summary>
        /// Do not schedule trigger events so that the call stack is maintained.
        /// </summary>
        /// <param name="tripArgs"></param>
        private void OnTriggered(TripEventArgs tripArgs)
        {
            try
            {
                // Add self to the route history.
                tripArgs.Waypoints.AddLast(new Waypoint(this));
                Triggered?.Invoke(this, tripArgs);
            }
            catch (Exception ex)
            {
                //TODO: remove self from route history?

                _logger.Error($"{ex.GetType().Name} during {nameof(Triggered)} event from {Name} trigger.", ex);
            }
        }
    }
}
