using CleanMachine.Interfaces;
using log4net;
using System;

namespace CleanMachine.Generic
{
    /// <summary>
    /// A trigger that listens for the StateExited event from a state machine.
    /// StateEntered is raised before the state's Enter & Do behaviors are done.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class StateExitedTrigger<TState> : TriggerBase where TState : struct
    {
        private readonly TState? _filterState;
        private readonly IState _state;

        //TODO: Change this to accept a collection like StateChangedTrigger
        public StateExitedTrigger(StateMachine<TState> source, TState? tripOnState, ILog logger)
            : base($"{typeof(StateMachine<TState>).Name}.{nameof(source.StateEntered)}<{typeof(StateExitedEventArgs<TState>).Name}>", source, logger)
        {
            if (tripOnState.HasValue)
            {
                _state = source[tripOnState.Value];
            }

            _filterState = tripOnState;
        }

        public StateExitedTrigger(string sourceName, StateMachine<TState> source, ILog logger)
            : this(source, null, logger)
        {
        }

        /// <summary>
        /// Gets the type cast base source.
        /// </summary>
        public StateMachine<TState> StateMachine
        {
            get { return base.Source as StateMachine<TState>; }
        }

        protected override void Enable()
        {
            if (_state == null)
            {
                StateMachine.StateExited += HandleMachineStateExited;
            }
            else
            {
                _state.ExitCompleted += HandleStateExited;
            }
        }

        protected override void Disable()
        {
            if (_state == null)
            {
                StateMachine.StateExited -= HandleMachineStateExited;
            }
            else
            {
                _state.ExitCompleted -= HandleStateExited;
            }
        }

        private void HandleMachineStateExited(object sender, StateExitedEventArgs<TState> args)
        {
            if (!_filterState.HasValue || args.State.Equals(_filterState.Value))
            {
                Trip(sender, args);
            }
        }

        private void HandleStateExited(object sender, StateExitedEventArgs args)
        {
            Trip(sender, args);
        }
    }
}
