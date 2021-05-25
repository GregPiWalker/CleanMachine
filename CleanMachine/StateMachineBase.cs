﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.ComponentModel;
using CleanMachine.Interfaces;
using log4net;
using Unity;
using Unity.Lifetime;
using NodaTime;

namespace CleanMachine
{
    //TODO: use Diversions.DivertingBindableBase as base class
    public abstract class StateMachineBase : IStateMachine, INotifyPropertyChanged
    {
        public const string EnteredStateKey = "EnteredState";
        public const string ExitedStateKey = "ExitedState";
        public const string EnteredOnKey = "EnteredOn";
        public const string ExitedOnKey = "ExitedOn";
        public const string BehaviorSchedulerKey = "BehaviorScheduler";
        public const string TriggerSchedulerKey = "TriggerScheduler";
        protected readonly List<Transition> _transitions = new List<Transition>();
        protected readonly List<State> _states = new List<State>();
        protected State _currentState;
        protected State _initialState;

        /// <summary>
        /// This is used for all synchronization constructs internal to this machine.  When triggers are synchronous
        /// they are not put on a serializing event queue, so they need some other mechanism for synchronization -
        /// hence the synchronizer.
        /// </summary>
        internal protected readonly object _synchronizer;

        /// <summary>
        /// Construct a machine with asynchronous triggering using the given scheduler.
        /// If no scheduler is provided, then triggering is done synchronously using the provided synchronizing object.
        /// If no synchronizer is provided, a default is supplied.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="runtimeContainer"></param>
        /// <param name="logger"></param>
        /// <param name="synchronizer">An object that is used to synchronize internal operation of this machine.</param>
        protected StateMachineBase(string name, IUnityContainer runtimeContainer, ILog logger, object synchronizer)
        {
            Name = name;
            Logger = logger;
            RuntimeContainer = runtimeContainer ?? new UnityContainer();

            // If a clock hasn't been provided, then register a standard system clock.
            if (!RuntimeContainer.HasTypeRegistration<IClock>())
            {
                RuntimeContainer.RegisterInstance<IClock>(SystemClock.Instance);
            }

            try
            {
                TriggerScheduler = RuntimeContainer.Resolve<IScheduler>(TriggerSchedulerKey);
                TriggerScheduler.Schedule(() => { bool dummy = true; });
                Logger.Debug($"{Name}:  was initialized with asynchronous triggers.");
            }
            catch
            {
                if (synchronizer == null)
                {
                    // When configured with synchronous triggers, this machine must have a local synchronization context.
                    Logger.Debug($"{Name}:  was initialized for synchronous operation using a default synchronization object.");
                    _synchronizer = new object();
                }
                else
                {
                    Logger.Debug($"{Name}:  was initialized for synchronous operation.");
                    _synchronizer = synchronizer;
                }
            }

            try
            {
                BehaviorScheduler = RuntimeContainer.Resolve<IScheduler>(BehaviorSchedulerKey);
                BehaviorScheduler.Schedule(() => { bool dummy = true; });
                Logger.Debug($"{Name}:  was initialized with asynchronous behaviors.");
            }
            catch
            {
                Logger.Debug($"{Name}:  was initialized with synchronous behaviors.");
            }
        }

        /// <summary>
        /// Construct a machine with synchronous triggers.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        protected StateMachineBase(string name, ILog logger)
            : this(name, null, logger, null)
        {
        }

        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        public IUnityContainer RuntimeContainer { get; }

        /// <summary>
        /// Gets this machine's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public ILog Logger { get; }

        public IState CurrentState => _currentState;

        public bool HasTriggerScheduler => TriggerScheduler != null;

        public bool HasBehaviorScheduler => BehaviorScheduler != null;

        internal protected IScheduler TriggerScheduler { get; }

        internal protected IScheduler BehaviorScheduler { get; }

        /// <summary>
        /// Gets a read-only collection of this machine's states.
        /// </summary>
        public ReadOnlyCollection<IState> States
        {
            get { return _states.Cast<IState>().ToList().AsReadOnly(); }
        }

        //public State FinalState { get; private set; }

        /// <summary>
        /// Gets the list of <see cref="IWaypoint"/>s that occurred in the most recent successful trip.
        /// </summary>
        public LinkedList<IWaypoint> History { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the machine should automatically attempt another
        /// state transition after a successful transition.  When the machine stimulates
        /// a state, only passive Transitions will be attempted.
        /// </summary>
        public bool AutoAdvance { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        internal bool Editable { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this machine is fully assembled yet.
        /// </summary>
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
        protected abstract void CreateStates(IEnumerable<string> stateNames);

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
            transition.SucceededInternal += HandleTransitionSucceededInternal;
            return transition;
        }

        /// <summary>
        /// Try to traverse exactly one outgoing transition from the current state
        /// that leads to the given desired state,
        /// looking for the first available transition whose guard condition succeeds.
        /// This ignores the passive quality of the attempted Transitions.
        /// </summary>
        /// <param name="toState"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal bool TryTransitionTo(string toState, TripEventArgs args)
        {
            var transitions = _currentState.FindTransitions(toState);
            var state = _currentState;
            foreach (var transition in transitions)
            {
                var attempted = AttemptTransition(transition, args);

                // This only tells whether a transition attempt was made.
                if (attempted.HasValue && attempted.Value)
                {
                    var result = state != _currentState;
                    if (result)
                    {
                        return result;
                    }
                }
            }

            return false;
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
        /// <param name="args"></param>
        protected abstract void OnStateChanged(TripEventArgs args);

        /// <summary>
        /// Forward the State's Entered event through this Machine's StateEntered event.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="args"></param>
        //protected abstract void OnStateEntered(StateEnteredEventArgs args);

        /// <summary>
        /// Perform state-exited work.
        /// Implementations of this method should be synchronous.  That is, they should avoid
        /// calling methods or raising events asynchronously.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected abstract void HandleStateExited(object sender, StateExitedEventArgs args);

        /// <summary>
        /// Perform state-entered work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected abstract void HandleStateEntered(object sender, StateEnteredEventArgs args);

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

        /// <summary>
        /// Signal this machine to stimulate its currently active transitions
        /// in case one of them is traversable.
        /// </summary>
        /// <param name="signalSource"></param>
        /// <returns></returns>
        public bool Signal(DataWaypoint signalSource)
        {
            var tripArgs = new TripEventArgs(_currentState.VisitIdentifier);
            tripArgs.Waypoints.AddLast(signalSource);

            //TODO: FILL IN BLANK?
            return Stimulate(tripArgs);
        }

        /// <summary>
        /// Stimulate the currently enabled passive transitions to attempt to exit the current state.
        /// 
        /// TODO: Change this? Only passive transitions are stimulated because presence of a trigger is
        /// taken to indicate that only the trigger should be able to stimulate the transition.
        /// </summary>
        /// <param name="signalSource"></param>
        /// <returns></returns>
        protected bool Stimulate(TripEventArgs tripArgs)
        {
            var passiveTransitions = _currentState.Transitions.Where(t => t.IsPassive).OfType<Transition>();
            foreach (var transition in passiveTransitions)
            {
                if (transition.AttemptTraverse(tripArgs))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to traverse the given transition.
        /// </summary>
        /// <param name="transition">The Transition to try to traverse.</param>
        /// <param name="args">The related TripEventArgs.</param>
        /// <returns>True if a transition attempt was made; false or null otherwise.  NOT an indicator for transition success.</returns>
        internal virtual bool? AttemptTransition(Transition transition, TripEventArgs args)
        {
            if (_synchronizer == null)
            {
                return AttemptTransitionUnsafe(transition, args);
            }
            else
            {
                // This lock regulates all transition triggers associated to the given synchronization context.
                // This means that only one of any number of transitions can successfully exit the current state,
                // whether those transitions all exist in one state machine or are distributed across a set of machines.
                lock (_synchronizer)
                {
                    return AttemptTransitionUnsafe(transition, args);
                }
            }
        }

        /// <summary>
        /// Jump into a specified state, circumventing the normal StateMachine operation.
        /// </summary>
        /// <param name="jumpTo"></param>
        internal virtual void JumpToState(State jumpTo)
        {
            if (jumpTo == null)
            {
                throw new ArgumentNullException("jumpTo");
            }

            if (_synchronizer == null)
            {
                JumpToStateUnsafe(jumpTo);
            }
            else
            {
                lock (_synchronizer)
                {
                    JumpToStateUnsafe(jumpTo);
                }
            }
        }

        protected void JumpToStateUnsafe(State jumpTo)
        {
            Logger.Debug($"{Name}:  jumping to state {jumpTo.Name}.");
            if (!jumpTo.CanEnter(null))
            {
                Logger.Warn($"{Name}:  state {jumpTo.Name} has false CanEnter value.");
                //throw new InvalidOperationException($"{Name}:  state {jumpTo.Name} could not be entered.");
            }

            //TODO: build some event args here
            var jumpArgs = new TripEventArgs(null);
            jumpArgs.Waypoints.AddLast(new DataWaypoint(this, nameof(JumpToState)));
            jumpTo.Enter(jumpArgs);
            _currentState = jumpTo;
            History = jumpArgs.Waypoints;

            // There is no transition to signal success on a jump, so need to fake it here.
            HandleTransitionSucceededInternal(this, jumpArgs);
        }

        /// <summary>
        /// The first successful transition will dispose of all other trigger handlers that were competing for action
        /// by disposing of the state selection context associated to the triggers.
        /// </summary>
        /// <param name="transition">The Transition to try to traverse.</param>
        /// <param name="args">The related TripEventArgs.</param>
        /// <returns>True if a transition attempt was made; false otherwise.  NOT an indicator for transition success.</returns>
        protected virtual bool AttemptTransitionUnsafe(Transition transition, TripEventArgs args)
        {
            if (transition != null && transition.AttemptTraverse(args))
            {
             
                

            }

            // A transition attempt was made.
            return true;
        }

        /// <summary>
        /// Run any activities that occur once the entire transition process is finished
        /// and the change is settled.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="args"></param>
        private void OnTransitionEnteredState(State state, TripEventArgs args)
        {
            if (!state.IsCurrentState)
            {
                //TODO: log
            }

            state.Settle(args);

            // When a state is entered successfully, auto-advance may be appropriate.
            if (AutoAdvance)
            {
                Stimulate(args);
            }
        }

        /// <summary>
        /// Update the current state in reponse to a successful transition that has entered
        /// its consumer state.
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="args"></param>
        private void OnStateEntryFinished(State state, TripEventArgs args)
        {
            _currentState = state;

            // Once a trip results in a state change, hold onto the route history.
            History = args.Waypoints;

            OnStateChanged(args);
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="name">The property name.</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Update the current state in reponse to a successful transition
        /// as signaled by the <see cref="Transition.SucceededInternal"/> event
        /// or by the <see cref="JumpToState(State)"/> signal.
        /// </summary>
        /// <param name="sender">Sender is the original signal source that triggers a transition.
        /// It might be a Transition or a StateMachine.</param>
        /// <param name="args"></param>
        protected virtual void HandleTransitionSucceededInternal(object sender, TripEventArgs args)
        {
            var transition = sender as Transition;
            var state = transition == null ? args.FindLastState() as State : transition.To;

            if (_synchronizer == null)
            {
                OnTransitionEnteredState(state, args);
            }
            else
            {
                // This lock regulates all transition triggers associated to the given synchronization context.
                // This means that only one of any number of transitions can successfully exit the current state,
                // whether those transitions all exist in one state machine or are distributed across a set of machines.
                lock (_synchronizer)
                {
                    OnTransitionEnteredState(state, args);
                }
            }
        }

        /// <summary>
        /// Run any activities that occur once the state entry is finished
        /// (as signaled by the State.EnteredInternal event),
        /// and the change is settled.
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="args"></param>
        protected virtual void HandleStateEnteredInternal(object sender, TripEventArgs args)
        {
            var state = sender as State;
            if (_synchronizer == null)
            {
                OnStateEntryFinished(state, args);
            }
            else
            {
                // This lock regulates all transition triggers associated to the given synchronization context.
                // This means that only one of any number of transitions can successfully exit the current state,
                // whether those transitions all exist in one state machine or are distributed across a set of machines.
                lock (_synchronizer)
                {
                    OnStateEntryFinished(state, args);
                }
            }
        }
    }
}
