using log4net;
using CleanMachine.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Reactive.Disposables;

namespace CleanMachine
{
    public abstract class StateMachine : IStateMachine
    {
        protected readonly List<Transition> _transitions = new List<Transition>();
        protected readonly List<State> _states = new List<State>();
        protected readonly IScheduler _behaviorScheduler;
        protected readonly IScheduler _transitionScheduler;
        protected State _currentState;
        protected State _initialState;
        private readonly object _synchronizationContext = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="globalSyncContext"></param>
        /// <param name="asynchronousTransitions">Indicates whether </param>
        /// <param name="asynchronousBehaviors">Indicates whether behaviors (ENTRY, EXIT, DO, EFFECT) are executed on
        /// a different thread from the state machine transitions and events.</param>
        protected StateMachine(string name, ILog logger, object globalSyncContext, bool asynchronousTransitions, bool asynchronousBehaviors)
        {
            _synchronizationContext = globalSyncContext;
            Name = name;
            Logger = logger;

            if (asynchronousTransitions)
            {
                // When configured with async transitions, this machine can operate with or without a synchronization context.
                _transitionScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Transition Scheduler", IsBackground = true }; });
            }
            else if (globalSyncContext == null)
            {
                // When configured with synchronous transitions, this machine must have a local synchronization context.
                Logger.Debug($"{Name}:  was initialized without transition synchronization.  This is not supported; a default synchronization context will be used.");
                _synchronizationContext = new object();
            }

            // If user requested asyncronous state & transition behaviors, assign a specific thread to a scheduler for the behaviors.
            if (asynchronousBehaviors)
            {
                _behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Behavior Scheduler", IsBackground = true }; });

                if (globalSyncContext != null)
                {
                    Logger.Warn($"{Name}:  inter-machine synchronization and asynchronous behaviors were both requested.  This is not recommended.  Using asynchronous behaviors could bypass aspects of machine sychronization.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public ILog Logger { get; }

        /// <summary>
        /// 
        /// </summary>
        public ReadOnlyCollection<IState> States
        {
            get { return _states.Cast<IState>().ToList().AsReadOnly(); }
        }

        //public State FinalState { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        internal bool Editable { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        internal bool IsAssembled { get; private set; }

        internal State CurrentState => _currentState;

        /// <summary>
        /// Set the machine's desired initial state.  This is enforced
        /// as a step in machine assembly so that initial state is defined in the same
        /// location as the rest of the machine structure.
        /// </summary>
        public void SetInitialState(string initialState)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"{Name} must be editable in order to set initial state.");
            }

            var state = FindState(initialState);
            if (state == null)
            {
                throw new ArgumentException($"{Name} does not contain a state named {initialState}.");
            }

            _initialState = state;
        }

        /// <summary>
        /// Take this machine out of edit mode, mark it as fully assembled and then
        /// enter the initial state.
        /// </summary>
        public void CompleteEdit()
        {
            if (!Editable || IsAssembled)
            {
                return;
            }

            IsAssembled = true;
            Editable = false;

            foreach (var state in _states)
            {
                state.CompleteEdit();
            }

            Logger.Debug($"{Name}:  editing disabled.");
            EnterInitialState();
        }

        /// <summary>
        /// Put this machine in edit mode, which allows you to assemble the structure.
        /// </summary>
        internal void Edit()
        {
            if (Editable || IsAssembled)
            {
                return;
            }

            foreach (var state in _states)
            {
                state.Edit();
            }

            Editable = true;
            Logger.Debug($"{Name}:  editing enabled.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateNames"></param>
        internal void CreateStates(IEnumerable<string> stateNames)
        {
            foreach (var stateName in stateNames)
            {
                var state = new State(stateName, Logger, _behaviorScheduler);
                _states.Add(state);
                state.EntryCompleted += OnStateEntered;
                state.ExitCompleted += OnStateExited;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="supplierState"></param>
        /// <param name="consumerState"></param>
        /// <returns></returns>
        internal Transition CreateTransition(string supplierState, string consumerState)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"{Name} must be in editable in order to create a new transition.");
            }

            var supplier = FindState(supplierState);
            if (supplier == null)
            {
                throw new InvalidOperationException($"{Name} does not contain state {supplierState}");
            }

            var consumer = FindState(consumerState);
            if (consumer == null)
            {
                throw new InvalidOperationException($"{Name} does not contain state {consumerState}");
            }

            var transition = supplier.CreateTransitionTo(Name, consumer);
            transition.Requested += HandleTransitionRequest;
            return transition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateName"></param>
        /// <returns></returns>
        protected State FindState(string stateName)
        {
            return _states.FirstOrDefault(s => s.Name == stateName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateName"></param>
        /// <returns></returns>
        protected bool ContainsState(string stateName)
        {
            return _states.Any(s => s.Name == stateName);
        }

        /// <summary>
        /// Perform state-changed work.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="args"></param>
        protected abstract void OnStateChanged(Transition transition, TriggerEventArgs args);

        /// <summary>
        /// Perform state-entered work.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected abstract void OnStateEntered(object sender, StateEnteredEventArgs args);

        /// <summary>
        /// Perform state-exited work.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected abstract void OnStateExited(object sender, StateExitedEventArgs args);

        /// <summary>
        /// Enter the Initial state and mark it as the current state.  Also, try
        /// to run to completion from the InitialNode.
        /// </summary>
        internal void EnterInitialState()
        {
            if (_initialState == null)
            {
                throw new InvalidOperationException($"{Name}:  initial state was not configured.");
            }

            if (!IsAssembled || Editable)
            {
                throw new InvalidOperationException($"{Name} must be fully assembled before it can enter the inital state.");
            }

            Logger.Info($"{Name}:  entering initial state {_initialState.Name}.");
            JumpToState(_initialState);
        }

        internal void JumpToState(State jumpTo)
        {
            if (jumpTo == null)
            {
                throw new ArgumentNullException("jumpTo");
            }

            if (_transitionScheduler == null)
            {
                JumpToStateSafe(jumpTo);
            }
            else
            {
                _transitionScheduler.Schedule(() => JumpToStateSafe(jumpTo));
            }
        }

        internal void AttemptTransition(TransitionEventArgs args)
        {
            if (_transitionScheduler == null)
            {
                AttemptTransitionSafe(args);
            }
            else
            { 
                _transitionScheduler.Schedule(args, (_, a) => { return AttemptTransitionSafe(a); });
            }
        }

        private void JumpToStateSafe(State jumpTo)
        {
            if (_synchronizationContext == null)
            {
                JumpToStateUnsafe(jumpTo);
            }
            else
            {
                lock (_synchronizationContext)
                {
                    JumpToStateUnsafe(jumpTo);
                }
            }
        }

        private void JumpToStateUnsafe(State jumpTo)
        {
            if (!jumpTo.CanEnter(null))
            {
                throw new InvalidOperationException($"{Name}:  state {jumpTo.Name} could not be entered.");
            }

            Logger.Debug($"{Name}:  jumping to state {jumpTo.Name}.");
            jumpTo.Enter(null);
            _currentState = jumpTo;

            OnStateChanged(null, new TriggerEventArgs() { Cause = this });
        }

        private IDisposable AttemptTransitionSafe(TransitionEventArgs args)
        {
            if (_synchronizationContext == null)
            {
                if (!AttemptTransitionUnsafe(args))
                {
                    return Disposable.Empty;
                }
            }
            else
            {
                // This lock regulates all transition triggers associated to the given synchronization context.
                // This means that only one of any number of transitions can successfully exit the current state,
                // whether those transitions all exist in one state machine or are distributed across a set of machines.
                lock (_synchronizationContext)
                {
                    if (!AttemptTransitionUnsafe(args))
                    {
                        return Disposable.Empty;
                    }
                }
            }

            return args.TriggerArgs.TriggerContext;
        }

        /// <summary>
        /// The first successful transition will dispose of all other trigger handlers that were competing for action
        /// by disposing of the state selection context associated to the triggers.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool AttemptTransitionUnsafe(TransitionEventArgs args)
        {
            if (args.TriggerArgs.TriggerContext.IsDisposed)
            {
                Logger.Debug($"{Name}.{nameof(AttemptTransitionSafe)}:  invalidating transition '{args.Transition.Name}' for trigger '{args.TriggerArgs.Trigger.ToString()}' due to a state change.");
                return false;
            }

            // Provide escape route in case the trigger became irrelevant while the handler for it was waiting.
            if (args.TriggerArgs.TriggerContext != _currentState.SelectionContext)
            {
                Logger.Debug($"{Name}.{nameof(AttemptTransitionSafe)}:  transition rejected for trigger '{args.TriggerArgs.Trigger.ToString()}'.  The trigger occurred in a different context of selection of state {_currentState.Name}.");
                return false;
            }

            // Provide escape route in case the trigger was deactivated while the handler for it was waiting.
            if (!args.TriggerArgs.Trigger.IsActive)
            {
                Logger.Debug($"{Name}.{nameof(AttemptTransitionSafe)}:  transition rejected for trigger '{args.TriggerArgs.Trigger.ToString()}'. Trigger is currently inactive.");
                return false;
            }

            if (args.Transition.AttemptTransition(args.TriggerArgs))
            {
                _currentState = args.Transition.To;
                OnStateChanged(args.Transition, args.TriggerArgs);
            }
            else
            {
                //OnTransitionFailed(transition, args);
            }

            // A transition attempt was made.
            return true;
        }

        private void HandleTransitionRequest(object sender, TriggerEventArgs args)
        {
            if (args.TriggerContext.IsDisposed)
            {
                return;
            }

            var currentState = _currentState;
            if (currentState == null || !currentState.IsEnabled)
            {
                return;
            }

            var transition = sender as Transition;
            var transitionArgs = transition.ToTransitionArgs(args);

            AttemptTransition(transitionArgs);
        }
    }
}
