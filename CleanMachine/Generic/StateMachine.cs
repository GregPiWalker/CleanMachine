using System;
using log4net;

namespace CleanMachine.Generic
{
    public sealed class StateMachine<TState> : StateMachine where TState : struct
    {
        public StateMachine(string name, ILog logger)
            : base(name, logger)
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

        protected override void OnStateChanged(Transition transition, TriggerEventArgs args)
        {
            if (StateChanged == null || transition == null)
            {
                return;
            }
            
            var changeArgs = transition.ToIStateChangedArgs<TState>(args);
            try
            {
                Logger.Debug($"StateMachine {Name}:  raising '{nameof(StateChanged)}' event.");
                StateChanged?.Invoke(this, changeArgs);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name} during '{nameof(StateChanged)}' event from {Name} state machine.", ex);
            }
        }

        ///// <summary>
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
                Logger.Debug($"StateMachine {Name}:  raising '{nameof(StateEntered)}' event.");
                StateEntered?.Invoke(this, args.ToStateEnteredArgs<TState>());
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
                Logger.Debug($"StateMachine {Name}:  raising '{nameof(StateExited)}' event.");
                StateExited?.Invoke(this, args.ToStateExitedArgs<TState>());
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType().Name} during '{nameof(StateExited)}' event from {Name} state machine.", ex);
            }
        }

        //protected override void OnTransitionFailed(Transition transition, TriggerEventArgs args)
        //{
        //    if (TransitionFailed == null)
        //    {
        //        return;
        //    }

        //    // Scheduling the events keeps them synchronized with the scheduled behaviors.
        //    var transitionArgs = args.ToITransitionArgs(transition);
        //    //_eventScheduler.Schedule(transitionArgs, (_, a) =>
        //    //{
        //        try
        //        {
        //            Logger.Debug($"StateMachine {Name}:  raising '{nameof(TransitionFailed)}' event.");
        //            TransitionFailed?.Invoke(this, transitionArgs);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error($"{ex.GetType().Name} during '{nameof(TransitionFailed)}' event from {Name} state machine.", ex);
        //        }

        //    //    return Disposable.Empty;
        //    //});
        //}

        //protected override void HandleStateEntryInitiated(object sender, StateEnteredEventArgs args)
        //{
        //    if (EnteringState == null)
        //    {
        //        return;
        //    }

        //    // Scheduling the events keeps them synchronized with the scheduled behaviors.
        //    //_eventScheduler.Schedule(args, (_, a) =>
        //    //{
        //        try
        //        {
        //            Logger.Debug($"StateMachine {Name}:  raising '{nameof(EnteringState)}' event.");
        //            EnteringState?.Invoke(this, args.ToIStateEnteredArgs<TState>());
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error($"{ex.GetType().Name} during '{nameof(EnteringState)}' event from {Name} state machine.", ex);
        //        }

        //    //    return Disposable.Empty;
        //    //});
        //}

        //protected override void HandleStateExitInitiated(object sender, StateExitedEventArgs args)
        //{
        //    if (ExitingState == null)
        //    {
        //        return;
        //    }

        //    // Scheduling the events keeps them synchronized with the scheduled behaviors.
        //    //_eventScheduler.Schedule(args, (_, a) =>
        //    //{
        //        try
        //        {
        //            Logger.Debug($"StateMachine {Name}:  raising '{nameof(ExitingState)}' event.");
        //            ExitingState?.Invoke(this, args.ToIStateExitedArgs<TState>());
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error($"{ex.GetType().Name} during '{nameof(ExitingState)}' event from {Name} state machine.", ex);
        //        }

        //    //    return Disposable.Empty;
        //    //});
        //}
    }
}
