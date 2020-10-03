using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CleanMachine.Generic
{
    public class StateMachine<TState> : StateMachineBase where TState : struct
    {
        public const string RequiredCommonStateValue = "Unknown";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        public StateMachine(string name, ILog logger)
            : this(name, logger, true)
        {
        }

        /// <summary>
        /// Create a <see cref="StateMachine{TState}"/> instance. This ctor
        /// gives a derived class the chance to delay state creation.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="createStates">Indicate whether this ctor should create state objects or not.</param>
        protected StateMachine(string name, ILog logger, bool createStates)
            : base(name, logger)
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

        public Interfaces.IState this[TState value]
        {
            get { return FindState(value); }
        }

        /// <summary>
        /// Try to traverse exactly one outgoing transition from the current state,
        /// looking for the first available transition whose guard condition succeeds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="callerName"></param>
        /// <returns>The current state - a new state if a transition succeeded; the prior existing state if not.</returns>
        public virtual TState TryTransition(object sender = null, [CallerMemberName] string callerName = null)
        {
            var args = new SignalEventArgs()
            {
                Cause = sender,
                Signal = callerName
            };

            TryTransitionTo(null, args);
            return CurrentState;
        }

        /// <summary>
        /// Try to traverse exactly one outgoing transition from the current state that leads to the supplied target state,
        /// looking for the first available transition whose guard condition succeeds.
        /// </summary>
        /// <param name="toState"></param>
        /// <param name="sender"></param>
        /// <param name="callerName"></param>
        /// <returns>True if a transition was traversed; false otherwise.</returns>
        public virtual bool TryTransitionTo(TState toState, object sender = null, [CallerMemberName] string callerName = null)
        {
            var args = new SignalEventArgs()
            {
                Cause = sender,
                Signal = callerName
            };

            return TryTransitionTo(toState.ToString(), args);
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
                var state = new State(stateName, Logger);
                _states.Add(state);
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

        /// <summary>
        /// Raise the <see cref="StateChanged"/> event synchronously.
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="args"></param>
        protected override void OnStateChanged(Transition transition, SignalEventArgs args)
        {
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void HandleStateEntered(object sender, Interfaces.StateEnteredEventArgs args)
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

        protected override void HandleStateExited(object sender, Interfaces.StateExitedEventArgs args)
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
