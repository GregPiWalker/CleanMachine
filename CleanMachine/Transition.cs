using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using CleanMachine.Interfaces;
using System.Linq;
using Unity;

namespace CleanMachine
{
    public class Transition : ITransition
    {
        protected readonly string _context;
        protected readonly List<TriggerBase> _triggers = new List<TriggerBase>();
        protected readonly Logger _logger;
        protected IConstraint _guard;
        protected IBehavior _effect;
        protected bool _enabled;
        private State _to;
        private State _from;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <param name="logger"></param>
        public Transition(string context, State fromState, State toState, Logger logger)
            : this(context, null, fromState, toState, logger)
        {
        }

        public Transition(string context, string stereotype, State fromState, State toState, Logger logger)
            : this(context, stereotype, logger)
        {
            if (fromState == null)
            {
                throw new ArgumentException($"{context}: transition cannot have a null supplier (fromState).");
            }

            if (toState == null)
            {
                throw new ArgumentException($"{context}: transition cannot have a null consumer (toState).");
            }

            From = fromState;
            To = toState;
        }

        protected Transition(string context, string stereotype, Logger logger)
        {
            _context = context;
            _logger = logger;
            Stereotype = stereotype;
        }

        /// <summary>
        /// Raised when an attempt to traverse this transition has succeeded.
        /// This occurs after the Enter and Exit operations on the consumer and supplier states.
        /// </summary>
        public virtual event EventHandler<TransitionEventArgs> Succeeded;

        /// <summary>
        /// Raised when an attempt to traverse this transition has failed.
        /// </summary>
        public virtual event EventHandler<TransitionEventArgs> Failed;

        /// <summary>
        /// Raised when this Transition needs to communicate internally a traversal success.
        /// </summary>
        internal event EventHandler<TripEventArgs> SucceededInternal;

        public string Name { get; protected set; }

        public string Stereotype { get; protected set; }

        public IState Consumer => To;

        public IState Supplier => From;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Transition"/> has any <see cref="ITrigger"/>s
        /// or not.  It is passive if there are no triggers.
        /// </summary>
        public virtual bool IsPassive => !_triggers.Any();

        internal protected State From
        {
            get => _from;
            protected set
            {
                _from = value;
                SetName();
            }
        }

        internal protected State To
        {
            get => _to;
            protected set
            {
                _to = value;
                SetName();
            }
        }

        internal IConstraint Guard
        {
            get { return _guard; }
            set
            {
                if (!Editable)
                {
                    throw new InvalidOperationException();
                }

                _guard = value;
            }
        }

        public IBehavior Effect
        {
            get { return _effect; }
            set
            {
                if (!Editable)
                {
                    throw new InvalidOperationException($"{GetType().Name} '{Name}' must be editable in order to set the effect in {_context}.");
                }
                if (value != null && RuntimeContainer == null)
                {
                    throw new InvalidOperationException($"{GetType().Name} '{Name}' must have a runtime container in order to set the effect in {_context}.");
                }

                _effect = value;
            }
        }

        internal protected bool Editable { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        internal protected IUnityContainer RuntimeContainer { get; set; }

        /// <summary>
        /// This is used for all synchronization constructs internal to this machine.  When triggers are synchronous
        /// they are not put on a serializing event queue, so they need some other mechanism for synchronization -
        /// hence the synchronizer.
        /// </summary>
        internal protected object GlobalSynchronizer { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"({_context}({Name}): ");
            for (int i = 0; i < _triggers.Count; i++)
            {
                sb.Append(_triggers[i].ToString());
                if (i + 1 < _triggers.Count)
                {
                    sb.Append(", ");
                }
            }

            if (Guard != null)
            {
                sb.Append(Guard.ToString());
            }

            if (Effect != null)
            {
                sb.Append(" / ").Append(Effect.ToString());
            }

            return sb.Append(")").ToString();
        }

        public void LogDiagnostics()
        {
            //TODO
        }

        public bool CanTransition(EventArgs sourceArgs)
        {
            if (!_enabled)
            {
                return false;
            }

            if (Guard == null)
            {
                return true;
            }

            // If the Guard has a generic parameter, then feed the supplied arguments into it.
            var constraint = Guard as Interfaces.Generic.IConstraint;
            if (constraint != null)
            {
                return constraint.IsTrue(sourceArgs);
            }

            return Guard.IsTrue();
        }

        public Transition AddTrigger(TriggerBase t)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"{GetType().Name} '{Name}' must be editable in order to add a trigger to {_context}.");
            }

            _triggers.Add(t);
            t.Triggered += HandleTrigger;
            return this;
        }

        internal void Edit()
        {
            Editable = true;
        }

        internal void CompleteEdit()
        {
            Editable = false;
        }

        /// <summary>
        /// Enable all non-lazy <see cref="TriggerBase"/>s.  Lazy triggers must explicitly be enabled later.
        /// The supplied visit ID will be fed back to a state when the trigger attempts to transit.  If the
        /// visit ID's match, then a transit may be allowed.
        /// </summary>
        /// <param name="visitId">The visitation identifier that uniquely describes the state entry that caused this invocation.</param>
        internal protected void Enable(IDisposable visitId)
        {
            if (_enabled)
            {
                return;
            }

            _enabled = true;
            _triggers.Where(t => !t.IsSourceLazy).ToList().ForEach(t => t.Activate(visitId));
        }

        internal protected void EnableLazyTriggers(IDisposable visitId)
        {
            _triggers.Where(t => t.IsSourceLazy).ToList().ForEach(t => t.Activate(visitId));
        }

        /// <summary>
        /// Disable all <see cref="TriggerBase"/>s and clear the current activation context.
        /// </summary>
        internal protected void Disable()
        {
            if (!_enabled)
            {
                return;
            }

            _enabled = false;
            _triggers.ForEach(t => t.Deactivate());
        }

        /// <summary>
        /// Attempt to transit this transition.  Transitting involves in order:
        /// 1) Validating the transition attempt.
        /// 2) Appending a new <see cref="Waypoint"/> to the <see cref="TripEventArgs"/>.
        /// 3) Exiting the supplier state.
        /// 4) Entering the consumer state.
        /// 5) Running the EFFECT if one exists.
        /// 6) Raising <see cref="Succeeded"/> event.
        /// </summary>
        /// <param name="args">TripEventArgs related to the attempt to transit.</param>
        /// <returns>True if a transit attempt was made; false otherwise.  NOT an indicator for transit success.</returns>
        internal protected virtual bool AttemptTransit(TripEventArgs args)
        {
            if (!ValidateAttempt(args))
            {
                return false;
            }

            var trigger = args.FindTrigger();
            if (trigger != null)
            {
                _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} transitting on behalf of '{trigger}' trigger.");
            }
            else
            {
                var origin = args.GetTripOrigin();
                // TODO: log signal name instead:
                _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} transitting due to signal from {origin.Juncture}.");
            }

            // Add self to the trip history.
            args.Waypoints.AddLast(new Waypoint(this));

            From.Exit(args);

            // After call to Enter(), the state machine's CurrentState property will be updated.
            // Also, all non-lazy outgoing triggers will be enabled.
            To.Enter(args);
            _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} old state exit and new state entry complete.");

            if (Effect != null)
            {
                _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} running EFFECT.");
                Effect?.Invoke(RuntimeContainer);
            }

            // After call to OnSucceeded(), the new state will be settled (DO behaviors executed/scheduled).
            OnSucceeded(args);

            // Now we can enable lazy outgoing triggers.  Delaying this as long as possible gives
            // any current state DO behaviors a chance to execute and potentially affect lazy trigger
            // source resolution.  However, if the DO is asynchronous, no guarantee of enough time is possible.
            To.EnableLazyTriggers();

            return true;
        }

        /// <summary>
        /// Validate that transit of this Transition is possible at present.
        /// Transit can only occur if the supplier State can be exited
        /// and the consumer State can be entered.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected bool ValidateAttempt(TripEventArgs args)
        {
            bool result = true;
            if (!CanTransition(args))
            {
                if (Guard != null)
                {
                    _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} transit inhibited by guard {Guard.ToString()}.");
                }

                result = false;
            }

            if (result)
            {
                if (!From.CanExit(this))
                {
                    _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} transition could not exit state {From.ToString()}.");
                    result = false;
                }
                else if (!To.CanEnter(this))
                {
                    _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} transition could not enter state {To.ToString()}.");
                    result = false;
                }
            }

            if (!result)
            {
                _logger.Trace($"{_context}: ({Name}).{nameof(AttemptTransit)} transit failed.");
                OnFailed(args);
                return false;
            }

            return true;
        }

        protected void SetName()
        {
            if (_to != null && _from != null)
            {
                //TODO: this isn't unique enough
                Name = $"{_from.Name}-->{_to.Name}";
            }
        }

        /// <summary>
        /// Raise the mandatory internal TraversalSucceeded event, and the optional
        /// Succeeded event.
        /// TraversalSucceeded signals the state machine to update itself relative to the new state.
        /// </summary>
        /// <param name="args"></param>
        protected void OnSucceeded(TripEventArgs args)
        {
            // This event is not optional, the StateMachine behavior depends on it.
            SucceededInternal?.Invoke(this, args);
            _logger.Trace($"{_context}: {GetType().Name} '{ToString()}' raising '{nameof(SucceededInternal)}' event.");

            try
            {
                Succeeded?.Invoke(this, args.ToTransitionArgs(this));
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} while raising '{nameof(Succeeded)}' event from '{Name}' transition in {_context}.", ex);
            }
        }

        protected void OnFailed(TripEventArgs args)
        {
            try
            {
                Failed?.Invoke(this, args.ToTransitionArgs(this));
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} while raising '{nameof(Failed)}' event from '{Name}' transition in {_context}.", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This implements a critical section because it is one of the ways that internal
        /// transitions are executed.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleTrigger(object sender, TripEventArgs args)
        {
            if (!_enabled)
            {
                return;
            }

            if (GlobalSynchronizer == null)
            {
                AttemptTransit(args);
            }
            else
            {
                // This lock regulates all transition triggers associated to the given synchronization context.
                // This means that only one of any number of transitions can successfully exit the current state,
                // whether those transitions all exist in one state machine or are distributed across a set of machines.
                lock (GlobalSynchronizer)
                {
                    AttemptTransit(args);
                }
            }
        }
    }
}
