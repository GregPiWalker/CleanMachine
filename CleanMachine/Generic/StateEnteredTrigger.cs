using CleanMachine.Interfaces;
using log4net;
using System;

namespace CleanMachine.Generic
{
    /// <summary>
    /// A trigger that listens for the StateEntered event from a state machine.
    /// StateEntered is raised before the state's Enter & Do behaviors are done.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class StateEnteredTrigger<TState> : TriggerBase where TState : struct
    {
        private readonly TState? _filterState;
        private readonly IState _state;

        public StateEnteredTrigger(StateMachine<TState> source, TState? tripOnState, ILog logger)
            : base($"{typeof(StateMachine<TState>).Name}.{nameof(source.StateEntered)}<{typeof(StateEnteredEventArgs<TState>).Name}>", source, logger)
        {
            if (tripOnState.HasValue)
            {
                _state = source[tripOnState.Value];
            }

            _filterState = tripOnState;
        }

        public StateEnteredTrigger(string sourceName, StateMachine<TState> source, ILog logger)
            : this(source, null, logger)
        {
        }

        public StateMachine<TState> StateMachine
        {
            get { return base.Source as StateMachine<TState>; }
        }

        protected override void Enable()
        {
            if (_state == null)
            {
                StateMachine.StateEntered += HandleSourceStateChanged;
            }
            else
            {
                _state.EntryCompleted += HandleStateEntered;
            }
        }
        
        protected override void Disable()
        {
            if (_state == null)
            {
                StateMachine.StateEntered -= HandleSourceStateChanged;
            }
            else
            {
                _state.EntryCompleted -= HandleStateEntered;
            }
        }

        private void HandleSourceStateChanged(object sender, StateEnteredEventArgs<TState> args)
        {
            if (!_filterState.HasValue || args.State.Equals(_filterState.Value))
            {
                Trip(sender, args);
            }
        }

        private void HandleStateEntered(object sender, StateEnteredEventArgs args)
        {
            Trip(sender, args);
        }
    }
}
