using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using CleanMachine.Interfaces.Generic;
using CleanMachine.Interfaces;

namespace CleanMachine
{
    public class Transition : ITransition
    {
        protected readonly string _context;
        protected readonly List<TriggerBase> _triggers = new List<TriggerBase>();
        protected readonly ILog _logger;
        protected Interfaces.IConstraint _guard;
        protected bool _enabled;
        protected IDisposable _activationContext;

        public Transition(string context, State fromState, State toState, ILog logger)
        {
            if (fromState == null || toState == null)
            {
                throw new ArgumentException($"{context} transition cannot have a null consumer or supplier state.");
            }

            _context = context;
            _logger = logger;
            From = fromState;
            To = toState;

            //TODO: this isn't unique enough
            Name = $"{From.Name}-->{To.Name}";
        }

        public virtual event EventHandler<Interfaces.TransitionEventArgs> Succeeded;
        public virtual event EventHandler<Interfaces.TransitionEventArgs> Failed;

        internal event EventHandler<SignalEventArgs> Requested;

        public string Name { get; private set; }

        public IState Consumer => To;

        public IState Supplier => From;

        internal State From { get; }

        internal State To { get; }

        internal Interfaces.IConstraint Guard
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

        protected bool Editable { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"\"{_context}({Name}): ");
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

            return sb.Append("\"").ToString();
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
                throw new InvalidOperationException($"Transition {Name} must be editable in order to add a trigger.");
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
        /// Enable all <see cref="TriggerBase"/>s and set the current activation context.
        /// </summary>
        /// <param name="stateSelectionContext">The new state selection context to hold as an activation context.</param>
        internal void Enable(IDisposable stateSelectionContext)
        {
            _activationContext = stateSelectionContext;
            _enabled = true;
            _triggers.ForEach(t => t.Activate());
        }

        /// <summary>
        /// Disable all <see cref="TriggerBase"/>s and clear the current activation context.
        /// </summary>
        internal void Disable()
        {
            _activationContext = null;
            _enabled = false;
            _triggers.ForEach(t => t.Deactivate());
        }

        /// <summary>
        /// Scheduling the Effect and events keeps the flow of external behaviors synchronized.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns>True if a transition attempt was made; false otherwise.  NOT an indicator for transition success.</returns>
        internal virtual bool AttemptTransition(TransitionEventArgs args)
        {
            if (!ValidateAttempt(args.SignalArgs))
            {
                return false;
            }

            if (args.SignalArgs is TriggerEventArgs)
            {
                _logger.Info($"{Name}.{nameof(AttemptTransition)}: transitioning on behalf of '{(args.SignalArgs as TriggerEventArgs).Trigger}' trigger.");
            }
            else
            {
                _logger.Info($"{Name}.{nameof(AttemptTransition)}: transitioning due to signal.");
            }

            From.Exit(this);
            To.Enter(args);
            _logger.Info($"{Name}.{nameof(AttemptTransition)}: transition complete.");

            OnSucceeded(args.SignalArgs);

            return true;
        }

        protected bool ValidateAttempt(SignalEventArgs args)
        {
            bool result = true;
            if (!CanTransition(args))
            {
                _logger.Debug($"{Name}.{nameof(AttemptTransition)}: transition inhibited by guard {Guard.ToString()}.");
                result = false;
            }

            if (result && (!To.CanEnter(this) || !From.CanExit(this)))
            {
                _logger.Debug($"{Name}.{nameof(AttemptTransition)}: transition could not enter state {To.ToString()} or exit state {From.ToString()}.");
                result = false;
            }

            if (!result)
            {
                _logger.Info($"{Name}.{nameof(AttemptTransition)}: transition failed.");
                OnFailed(args);
                return false;
            }

            return true;
        }

        protected void OnSucceeded(SignalEventArgs args)
        {
            try
            {
                var transitionArgs = args.ToITransitionArgs(this);
                Succeeded?.Invoke(this, transitionArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} while raising '{nameof(Succeeded)}' event from {Name} transition.", ex);
            }
        }

        protected void OnFailed(SignalEventArgs args)
        {
            try
            {
                var transitionArgs = args.ToITransitionArgs(this);
                Failed?.Invoke(this, transitionArgs);
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} while raising '{nameof(Failed)}' event from {Name} transition.", ex);
            }
        }

        /// <summary>
        /// Raises the events that indicate a request to transition from supplier state to consumer state.
        /// </summary>
        /// <param name="args"></param>
        private void OnRequested(TriggerEventArgs args)
        {
            _logger.Debug($"Transition {ToString()}: raising '{nameof(Requested)}' event.");

            // Tag the args with the current transition activation context so that other requests
            // can be validated against a particular state selection occurrence.
            args.TriggerContext = _activationContext;

            // This event is not optional, the StateMachine behavior depends on it.
            Requested?.Invoke(this, args);
        }

        private void HandleTrigger(object sender, TriggerEventArgs args)
        {
            if (!_enabled)
            {
                return;
            }

            // Just forward it on as a request to transition.
            OnRequested(args);
        }
    }
}
