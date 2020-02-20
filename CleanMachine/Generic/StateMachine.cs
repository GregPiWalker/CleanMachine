using System;
using log4net;

namespace CleanMachine.Generic
{
    public sealed class StateMachine<TState> : StateMachine where TState : struct
    {
        public StateMachine(string name, ILog logger)
            : this(name, logger, null, false)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="synchronizationContext"></param>
        /// <param name="asynchronousBehaviors">Indicates whether behaviors (ENTRY, EXIT, DO, EFFECT) are executed on
        /// a different thread from the state machine transitions and events.</param>
        public StateMachine(string name, ILog logger, object synchronizationContext, bool asynchronousBehaviors)
            : base(name, logger, synchronizationContext, asynchronousBehaviors)
        {
            CreateStates();
        }

        /// <summary>
        /// Raised after 
        /// </summary>
        public event EventHandler<StateChangedEventArgs<TState>> StateChanged;
        public event EventHandler<StateEnteredEventArgs<TState>> StateEntered;
        public event EventHandler<StateExitedEventArgs<TState>> StateExited;

        public TState CurrentState => _currentState.ToEnum<TState>();

        public Interfaces.IState this[TState value]
        {
            get { return FindState(value); }
        }

        private void CreateStates()
        {
            var stateNames = typeof(TState).GetEnumNames();

            CreateStates(stateNames);
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

        internal Transition CreateTransition(TState supplierState, TState consumerState)
        {
            return CreateTransition(supplierState.ToString(), consumerState.ToString());
        }

        internal State FindState(TState state)
        {
            return FindState(state.ToString());
        }

        /// <summary>
        /// Raise the <see cref="StateChanged"/> event synchronously.
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="args"></param>
        protected override void OnStateChanged(Transition transition, TriggerEventArgs args)
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
        protected override void OnStateEntered(object sender, Interfaces.StateEnteredEventArgs args)
        {
            if (StateEntered == null)
            {
                return;
            }

            try
            {
                Logger.Debug($"{Name}:  raising '{nameof(StateEntered)}' event.");
                StateEntered?.Invoke(this, args.ToStateEnteredArgs<TState>());
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name} during '{nameof(StateEntered)}' event from {Name} state machine.", ex);
            }
        }

        protected override void OnStateExited(object sender, Interfaces.StateExitedEventArgs args)
        {
            if (StateExited == null)
            {
                return;
            }

            try
            {
                Logger.Debug($"{Name}:  raising '{nameof(StateExited)}' event.");
                StateExited?.Invoke(this, args.ToStateExitedArgs<TState>());
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name} during '{nameof(StateExited)}' event from {Name} state machine.", ex);
            }
        }
    }
}
