using System;
using log4net;

namespace CleanMachine.Generic
{
    public sealed class StateMachine<TState> : StateMachine where TState : struct
    {
        /// <summary>
        /// Create a fully asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// internal transitions.  Another scheduler with a dedicated background thread is instantiated for running
        /// the following behaviors: ENTRY, DO, EXIT, EFFECT.  Both schedulers serialize their workflow, but will
        /// operate asynchronously with respect to each other, as well as with respect to incoming trigger invocations.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static StateMachine<TState> CreateAsync(string name, ILog logger)
        {
            return new StateMachine<TState>(name, logger, null, true, true);
        }

        /// <summary>
        /// Create a partially asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// internal transitions.  UML behaviors (ENTRY, DO, EXIT, EFFECT) are executed synchronously on the same transition thread.
        /// The scheduler serializes its workflow, but will operate asynchronously with respect to incoming trigger invocations.
        /// This configuration gives you an option of supplying a global synchronization context that can be used to synchronize
        /// transitions (state changes) across multiple state machines.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="globalSyncContext">If global transition synchronization across multiple <see cref="StateMachine"/>s is desired,
        /// supply a synchronization context here. Otherwise, supply a null value.</param>
        /// <returns></returns>
        public static StateMachine<TState> CreatePartialAsync(string name, ILog logger, object globalSyncContext = null)
        {
            return new StateMachine<TState>(name, logger, globalSyncContext, true, false);
        }

        /// <summary>
        /// Create a StateMachine that transitions synchronously.  An option is given whether to make the UML behaviors
        /// (ENTRY, DO, EXIT, EFFECT) synchronous or not.  If asynchronous behaviors is chosen, a scheduler with a 
        /// dedicated background thread is instantiated for running them.  This optional scheduler serializes its workflow,
        /// but will operate asynchronously with respect to transitions and incoming trigger invocations.
        /// If synchronous behaviors is chosen, then transitions, behaviors and trigger invocations will all occur
        /// on the current thread.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="asynchronousBehaviors"></param>
        /// <returns></returns>
        public static StateMachine<TState> Create(string name, ILog logger, bool asynchronousBehaviors)
        {
            return new StateMachine<TState>(name, logger, null, false, asynchronousBehaviors);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="globalSyncContext"></param>
        /// <param name="asynchronousTransitions">Indicates whether </param>
        /// <param name="asynchronousBehaviors">Indicates whether behaviors (ENTRY, EXIT, DO, EFFECT) are executed on
        /// a different thread from the state machine transitions and events.</param>
        internal StateMachine(string name, ILog logger, object globalSyncContext, bool asynchronousTransitions, bool asynchronousBehaviors)
            : base(name, logger, globalSyncContext, asynchronousTransitions, asynchronousBehaviors)
        {
            CreateStates();
        }

        /// <summary>
        /// Raised after 
        /// </summary>
        public event EventHandler<StateChangedEventArgs<TState>> StateChanged;
        public event EventHandler<StateEnteredEventArgs<TState>> StateEntered;
        public event EventHandler<StateExitedEventArgs<TState>> StateExited;

        public new TState CurrentState => _currentState.ToEnum<TState>();

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
