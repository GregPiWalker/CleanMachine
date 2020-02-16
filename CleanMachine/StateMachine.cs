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
        //private readonly object _transitionLock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="behaveAsync">Indicates whether behaviors are executed on a different thread from the
        /// state machine transitions and events.  Defaults to false.</param>
        public StateMachine(string name, ILog logger, bool behaveAsync = false)
        {
            Name = name;
            Logger = logger;

            _transitionScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Transition Scheduler", IsBackground = true }; });
            if (behaveAsync)
            {
                _behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{Name} Behavior Scheduler", IsBackground = true }; });
            }
        }

        //public event EventHandler<Interfaces.TriggerEventArgs> TriggerOccurred;

        public string Name { get; }

        public ILog Logger { get; }

        public ReadOnlyCollection<IState> States
        {
            get { return _states.Cast<IState>().ToList().AsReadOnly(); }
        }

        //public State FinalState { get; private set; }

        internal bool Editable { get; private set; }

        internal bool IsAssembled { get; private set; }

        /// <summary>
        /// Set the machine's desired initial state.  This is enforced
        /// as a step in machine assembly so that initial state is defined in the same
        /// location as the rest of the machine structure.
        /// </summary>
        public void SetInitialState(string initialState)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"StateMachine {Name} must be editable in order to set initial state.");
            }

            var state = FindState(initialState);
            if (state == null)
            {
                throw new ArgumentException($"StateMachine {Name} does not contain a state named {initialState}.");
            }

            _initialState = state;
        }

        /// <summary>
        /// Take this machine out of edit mode, mark it as fully assembled and then
        /// enter the initial state.
        /// </summary>
        public void CompleteEdit()
        {
            if (IsAssembled)
            {
                return;
            }

            IsAssembled = true;
            Editable = false;

            foreach (var state in _states)
            {
                state.CompleteEdit();
            }

            Logger.Debug($"StateMachine {Name}:  editing disabled.");
            EnterInitialState();
        }

        /// <summary>
        /// Put this machine in edit mode, which allows you to assemble the structure.
        /// </summary>
        internal void Edit()
        {
            if (IsAssembled)
            {
                return;
            }

            foreach (var state in _states)
            {
                state.Edit();
            }

            Editable = true;
            Logger.Debug($"StateMachine {Name}:  editing enabled.");
        }

        protected void CreateStates(IEnumerable<string> stateNames)
        {
            foreach (var stateName in stateNames)
            {
                var state = new State(stateName, Logger, _behaviorScheduler);
                _states.Add(state);
                state.EntryCompleted += HandleStateEntered;
                state.ExitCompleted += HandleStateExited;
            }
        }

        protected Transition CreateTransition(string supplierState, string consumerState)
        {
            if (!Editable)
            {
                throw new InvalidOperationException($"StateMachine {Name} must be in editable in order to create a new transition.");
            }

            var supplier = FindState(supplierState);
            if (supplier == null)
            {
                throw new InvalidOperationException($"StateMachine {Name} does not contain state {supplierState}");
            }

            var consumer = FindState(consumerState);
            if (consumer == null)
            {
                throw new InvalidOperationException($"StateMachine {Name} does not contain state {consumerState}");
            }

            var transition = supplier.CreateTransitionTo(Name, consumer);
            transition.Requested += HandleTransitionRequest;
            return transition;
        }

        protected State FindState(string stateName)
        {
            return _states.FirstOrDefault(s => s.Name == stateName);
        }

        protected bool ContainsState(string stateName)
        {
            return _states.Any(s => s.Name == stateName);
        }

        protected abstract void OnStateChanged(Transition transition, TriggerEventArgs args);

        protected abstract void HandleStateEntered(object sender, StateEnteredEventArgs args);

        protected abstract void HandleStateExited(object sender, StateExitedEventArgs args);

        /// <summary>
        /// Enter the Initial state and mark it as the current state.  Also, try
        /// to run to completion from the InitialNode.
        /// </summary>
        private void EnterInitialState()
        {
            if (_initialState == null)
            {
                throw new InvalidOperationException($"StateMachine {Name}:  initial state was not configured.");
            }

            _transitionScheduler.Schedule(() =>
            {
                //lock (_transitionLock)
                //{
                if (!_initialState.CanEnter(null))
                {
                    throw new InvalidOperationException($"StateMachine {Name}:  initial state {_initialState.Name} could not be entered.");
                }

                Logger.Debug($"StateMachine {Name}:  entering initial state {_initialState.Name}.");
                _initialState.Enter(null);
                _currentState = _initialState;

                OnStateChanged(null, new TriggerEventArgs() { Cause = this });
                
                //}
            });
        }

        private IDisposable AttemptTransition(TransitionEventArgs args)
        {
            if (args.TriggerArgs.TriggerContext.IsDisposed)
            {
                Logger.Debug($"{Name}.{nameof(AttemptTransition)}: invalidating transition '{args.Transition.Name}' for trigger '{args.TriggerArgs.Trigger.ToString()}' due to a state change.");
                return Disposable.Empty;
            }

            // This regulates all transition triggers so that only one can lead to success.
            //lock (_transitionLock)
            //{
                // Provide escape route in case the trigger was deactivated while the handler for it was waiting.
                if (args.TriggerArgs.TriggerContext != _currentState.SelectionContext)
                {
                    Logger.Debug($"{Name}.{nameof(AttemptTransition)}: transition rejected for trigger '{args.TriggerArgs.Trigger.ToString()}'.  The trigger occurred in a different context of selection of state {_currentState.Name}.");
                    return Disposable.Empty;
                }

                // Provide escape route in case the trigger was deactivated while the handler for it was waiting.
                if (!args.TriggerArgs.Trigger.IsActive)
                {
                    //TODO: this may not be a valid case anymore since I removed all async internal operations
                    Logger.Debug($"{Name}.{nameof(AttemptTransition)}: transition rejected for trigger '{args.TriggerArgs.Trigger.ToString()}'. Trigger is currently inactive.");
                    return Disposable.Empty;
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
            //}

            return args.TriggerArgs.TriggerContext;
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
            
            // This regulates all transition triggers so that only one can lead to success.  The first
            // successful transition will dispose of all other trigger handlers that were competing for action.
            _transitionScheduler.Schedule(transitionArgs, (_, a) => { return AttemptTransition(a); });
        }
    }
}
