using CleanMachine.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using Unity;

namespace CleanMachine.Generic
{
    public class StateMachine<TState> : StateMachineBase where TState : struct
    {
        public const string RequiredCommonStateValue = "Unknown";

        /// <summary>
        /// Construct a machine with asynchronous triggers and populate its states.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="triggerScheduler">The IScheduler used by all triggers in this machine.</param>
        /// <param name="behaviorScheduler">The IScheduler used by any default behaviors in this machine.</param>
        public StateMachine(string name, ILog logger)
            : this(name, logger, true)
        {
        }

        /// <summary>
        /// Create a <see cref="StateMachine{TState}"/> instance with asynchronous triggers.
        /// This ctor gives a derived class the chance to delay state creation.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="triggerScheduler">The IScheduler used by all triggers in this machine.</param>
        /// <param name="behaviorScheduler">The IScheduler used by any default behaviors in this machine.</param>
        /// <param name="createStates">Indicate whether this ctor should create state objects or not.</param>
        public StateMachine(string name, ILog logger, bool createStates)
            : base(name, null, logger, null)
        {
            if (createStates)
            {
                CreateStates(typeof(TState).GetEnumNames());
            }
        }

        /// <summary>
        /// Create a <see cref="StateMachine{TState}"/> instance with synchronous triggers.
        /// This ctor gives a derived class the chance to delay state creation.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="runtimeContainer"></param>
        /// <param name="logger"></param>
        /// <param name="createStates">Indicate whether this ctor should create state objects or not.</param>
        /// <param name="synchronizer">An object that is used to synchronize internal operation of this machine when a trigger <see cref="IScheduler"/> is not supplied.</param>
        public StateMachine(string name, IUnityContainer runtimeContainer, ILog logger, bool createStates, object synchronizer)
            : base(name, runtimeContainer, logger, synchronizer)
        {
            if (createStates)
            {
                CreateStates(typeof(TState).GetEnumNames());
            }
        }

        public virtual event EventHandler<StateChangedEventArgs<TState>> StateChanged;
        public virtual event EventHandler<StateEnteredEventArgs<TState>> StateEntered;
        public virtual event EventHandler<StateExitedEventArgs<TState>> StateExited;

        public new TState CurrentState => _currentState == null ? RequiredCommonStateValue.ToEnum<TState>() : _currentState.ToEnum<TState>();

        public IState this[TState value]
        {
            get { return FindState(value); }
        }

        /// <summary>
        /// Try to traverse exactly one outgoing transition from the current state,
        /// looking for the first available transition whose guard condition succeeds.
        /// This ignores the passive quality of the attempted Transitions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="callerName">Name of the calling method. (supplied by runtime).</param>
        /// <returns>The current state - a new state if a transition succeeded; the prior existing state if not.</returns>
        public virtual TState TryTransition(object sender = null, [CallerMemberName] string callerName = null)
        {
            var tripArgs = new TripEventArgs(new BlankDisposable());
            tripArgs.Waypoints.AddLast(new DataWaypoint(sender, callerName));

            TryTransitionTo(null, tripArgs);
            return CurrentState;
        }

        /// <summary>
        /// Try to traverse exactly one outgoing transition from the current state that leads to the supplied target state,
        /// looking for the first available transition whose guard condition succeeds.
        /// This ignores the passive quality of the attempted Transitions.
        /// </summary>
        /// <param name="toState"></param>
        /// <param name="sender"></param>
        /// <param name="callerName">Name of the calling method. (supplied by runtime).</param>
        /// <returns>True if a transition was traversed; false otherwise.</returns>
        public virtual bool TryTransitionTo(TState toState, object sender = null, [CallerMemberName] string callerName = null)
        {
            var tripArgs = new TripEventArgs(new BlankDisposable());
            tripArgs.Waypoints.AddLast(new DataWaypoint(sender, callerName));

            return TryTransitionTo(toState.ToString(), tripArgs);
        }

        /// <summary>
        /// Set the machine's desired initial state.  This is enforced
        /// as a step in machine assembly so that initial state is defined in the same
        /// location as the rest of the machine structure.
        /// </summary>
        public void SetInitialState(TState initialState)
        {
            SetInitialState(initialState.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateNames"></param>
        protected override void CreateStates(IEnumerable<string> stateNames)
        {
            if (!stateNames.Any(name => name.Equals(RequiredCommonStateValue)))
            {
                throw new InvalidOperationException($"StateMachine requires a state enum that contains the value {RequiredCommonStateValue}.");
            }

            foreach (var stateName in stateNames)
            {
                var state = new State(stateName, RuntimeContainer, Logger);
                _states.Add(state);
                state.EnteredInternal += HandleStateEnteredInternal;
                state.Entered += HandleStateEntered;
                state.Exited += HandleStateExited;
            }
        }

        internal State FindState(TState state)
        {
            return FindState(state.ToString());
        }

        internal Transition CreateTransition(TState supplierState, TState consumerState)
        {
            return CreateTransition(supplierState.ToString(), consumerState.ToString());
        }

        protected override bool AttemptTransitionUnsafe(Transition transition, TripEventArgs args)
        {
            bool result = base.AttemptTransitionUnsafe(transition, args);
            if (result)
            {
                //TODO: beware auto-advance stepping through multiple states
                OnPropertyChanged(nameof(CurrentState));
            }

            return result;
        }

        /// <summary>
        /// Raise the <see cref="StateChanged"/> event synchronously.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStateChanged(TripEventArgs args)
        {
            var transition = args.FindLastTransition() as Transition;
            if (StateChanged == null || transition == null)
            {
                return;
            }

            var changeArgs = transition.ToIStateChangedArgs<TState>(args);
            try
            {
                Logger.Debug($"{Name}:  raising '{nameof(StateChanged)}' event.");
                StateChanged?.Invoke(this, changeArgs);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name} during '{nameof(StateChanged)}' event from {Name} state machine.", ex);
            }
        }

        /// <summary>
        /// Forward the State's Entered event through this Machine's StateEntered event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        //protected override void OnStateEntered(StateEnteredEventArgs args)
        //{
        //}

        /// <summary>
        /// Forward the State's Entered event through this Machine's StateEntered event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void HandleStateEntered(object sender, StateEnteredEventArgs args)
        {
            if (StateEntered == null)
            {
                return;
            }

            try
            {
                var enteredArgs = args.ToStateEnteredArgs<TState>();
                Logger.Debug($"{Name}:  raising '{nameof(StateEntered)}' event with {enteredArgs.State}.");
                StateEntered?.Invoke(this, enteredArgs);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name} during '{nameof(StateEntered)}' event from {Name} state machine.", ex);
            }
        }

        /// <summary>
        /// Forward the State's Exited event through this Machine's StateExited event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void HandleStateExited(object sender, StateExitedEventArgs args)
        {
            if (StateExited == null)
            {
                return;
            }

            try
            {
                var exitedArgs = args.ToStateExitedArgs<TState>();
                Logger.Debug($"{Name}:  raising '{nameof(StateExited)}' event with {exitedArgs.State}.");
                StateExited?.Invoke(this, exitedArgs);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name} during '{nameof(StateExited)}' event from {Name} state machine.", ex);
            }
        }
    }
}
