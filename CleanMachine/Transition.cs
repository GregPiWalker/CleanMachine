using System;
using System.Collections.Generic;
using CleanMachine.Interfaces;
using System.Text;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using log4net;

namespace CleanMachine
{
    public class Transition : ITransition
    {
        private readonly List<TriggerBase> _triggers = new List<TriggerBase>();
        private readonly string _context;
        private Constraint _guard;
        private Action _effect;
        private bool _enabled;
        private object _activationContext;
        protected readonly IScheduler _scheduler;
        protected readonly ILog _logger;

        public Transition(string context, State fromState, State toState, IScheduler scheduler, ILog logger)
        {
            _context = context;
            _scheduler = scheduler;
            _logger = logger;
            From = fromState;
            To = toState;
            if (From == null || To == null)
            {
                throw new ArgumentException($"{context} transition cannot have a null consumer or supplier state.");
            }

            //TODO: this isn't unique enough
            Name = $"{From.Name}-->{To.Name}";
        }

        public event EventHandler<Interfaces.TransitionEventArgs> Succeeded;
        public event EventHandler<Interfaces.TransitionEventArgs> Failed;
        //public event EventHandler<Interfaces.TriggerEventArgs> TriggerOccurred;
        internal event EventHandler<TriggerEventArgs> Requested;

        public string Name { get; private set; }

        public IState Consumer { get { return To; } }

        public IState Supplier { get { return From; } }

        internal State From { get; }

        internal State To { get; }

        internal Constraint Guard
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

        internal Action Effect
        {
            get { return _effect; }
            set
            {
                if (!Editable)
                {
                    throw new InvalidOperationException();
                }

                _effect = value;
            }
        }

        internal bool Editable { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"{_context}({Name}): ");
            for (int i = 0; i < _triggers.Count; i++)
            {
                sb.Append(_triggers[i].ToString());
                if (i + 1 < _triggers.Count)
                {
                    sb.Append(", ");
                }
            }
            foreach (var trigger in _triggers)
            {
            }

            if (Guard != null)
            {
                sb.Append(Guard.ToString());
            }
            
            if (Effect != null)
            {
                sb.Append(" / ").Append(Effect.ToString());
            }

            return sb.ToString();
        }

        public virtual bool CanTransition(EventArgs sourceArgs)
        {
            return _enabled && (Guard == null || Guard.IsTrue());
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
        /// Scheduling the Effect and events keeps the flow of external behaviors synchronized.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal bool AttemptTransition(TriggerEventArgs args)
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

            From.Exit(this);
            To.Enter(this);
            _logger.Info($"{Name}.{nameof(AttemptTransition)}: transition succeeded.");
            
            if (Effect != null)
            {
                _logger.Debug($"{Name}.{nameof(AttemptTransition)}: running EFFECT.");
                if (_scheduler == null)
                {
                    Effect?.Invoke();
                }
                else
                {
                    _scheduler.Schedule(Effect);
                }
            }

            OnSucceeded(args);

            return true;
        }

        /// <summary>
        /// Enable all <see cref="TriggerBase"/>s and set the current activation context.
        /// </summary>
        /// <param name="stateSelectionContext">The new state selection context to hold as an activation context.</param>
        internal void Enable(object stateSelectionContext)
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

        private void HandleTrigger(object sender, TriggerEventArgs args)
        {
            if (!_enabled)
            {
                return;
            }

            // Just forward it on as a request to transition.
            OnRequested(args);
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

        private void OnSucceeded(TriggerEventArgs args)
        {
            try
            {
                var transitionArgs = args.ToITransitionArgs(this);
                Succeeded?.Invoke(this, transitionArgs);
            }
            catch (Exception ex)
            {
                //TODO: log it
            }
        }

        private void OnFailed(TriggerEventArgs args)
        {
            try
            {
                var transitionArgs = args.ToITransitionArgs(this);
                Failed?.Invoke(this, transitionArgs);
            }
            catch (Exception ex)
            {
                //TODO: log it
            }
        }
    }
}
