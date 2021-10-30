using CleanMachine.Interfaces;
using CleanMachine.Interfaces.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity;
using log4net;

namespace CleanMachine.Generic
{
    public class StateMachine<TState> : StateMachineBase, IStateMachine<TState> where TState : struct
    {
        public const string RequiredCommonStateValue = "Unknown";

        /// <summary>
        /// Construct a machine with asynchronous triggers and populate its states.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
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
        /// <param name="createStates">Indicate whether this ctor should create state objects or not.</param>
        public StateMachine(string name, ILog logger, bool createStates)
            : base(name, null, logger)
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
        public StateMachine(string name, IUnityContainer runtimeContainer, ILog logger, bool createStates)
            : base(name, runtimeContainer, logger)
        {
            if (createStates)
            {
                CreateStates(typeof(TState).GetEnumNames());
            }
        }

        public virtual new event EventHandler<StateChangedEventArgs<TState>> StateChanged;
        public virtual event EventHandler<StateEnteredEventArgs<TState>> StateEntered;
        public virtual event EventHandler<StateExitedEventArgs<TState>> StateExited;

        public new TState CurrentState => _currentState == null ? RequiredCommonStateValue.ToEnum<TState>() : _currentState.ToEnum<TState>();

        public State this[TState value]
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
            var tripArgs = new TripEventArgs(_currentState.VisitIdentifier, new DataWaypoint(sender, callerName));
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
            var tripArgs = new TripEventArgs(_currentState.VisitIdentifier, new DataWaypoint(sender, callerName));
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

        internal State FindState(TState state)
        {
            return FindState(state.ToString());
        }

        internal Transition CreateTransition(TState supplierState, TState consumerState)
        {
            return CreateTransition(supplierState.ToString(), consumerState.ToString());
        }

        public /*internal*/ void JumpToState(TState jumpTo)
        {
            JumpToState(FindState(jumpTo));
        }

        protected override void Dispose(bool disposing)
        {
            StateChanged = null;
            StateEntered = null;
            StateExited = null;

            base.Dispose(disposing);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateNames"></param>
        protected virtual void CreateStates(IEnumerable<string> stateNames)
        {
            if (!stateNames.Any(name => name.Equals(RequiredCommonStateValue)))
            {
                throw new InvalidOperationException($"{Name}: StateMachine requires a state enum that contains the value {RequiredCommonStateValue}.");
            }

            foreach (var stateName in stateNames)
            {
                var state = new State(stateName, Name, RuntimeContainer, Logger);
                _states.Add(state);
                state.EnteredInternal += HandleStateEnteredInternal;
                state.Entered += HandleStateEntered;
                state.Exited += HandleStateExited;
            }
        }

        /// <summary>
        /// Raise the <see cref="StateChanged"/> event synchronously.
        /// </summary>
        /// <param name="previousState"></param>
        /// <param name="args"></param>
        protected override void OnStateChanged(State previousState, TripEventArgs args)
        {
            if (previousState == null)
            {
                // Don't raise events for entry into the initial state.
                return;
            }

            if (StateChanged != null)
            {
                StateChangedEventArgs<TState> changeArgs = null;
                var transition = args.FindLastTransition() as Transition;
                if (transition == null)
                {
                    changeArgs = new StateChangedEventArgs<TState>()
                        {
                            ResultingState = _currentState.ToEnum<TState>(),
                            PreviousState = previousState.ToEnum<TState>(),
                            //TransitionArgs = null
                        };
                }
                else
                {
                    changeArgs = transition.ToIStateChangedArgs<TState>(args);
                }

                Logger.Debug($"{Name}:  raising '{nameof(StateChanged)}' event.");
                try
                {
                    StateChanged?.Invoke(this, changeArgs);
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex.GetType().Name} during '{nameof(StateChanged)}'<> event from {Name} state machine.", ex);
                }
            }

            RaiseStateChangedBase(new StateChangedEventArgs() { PreviousState = previousState, ResultingState = _currentState });
        }

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
