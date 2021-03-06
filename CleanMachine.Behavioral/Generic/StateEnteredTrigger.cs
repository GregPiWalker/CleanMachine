﻿using CleanMachine.Generic;
using CleanMachine.Interfaces;
using log4net;
using System.Reactive.Concurrency;

namespace CleanMachine.Behavioral.Generic
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

        //TODO: Change this to accept a collection like StateChangedTrigger
        public StateEnteredTrigger(StateMachine<TState> source, TState? tripOnState, IScheduler tripScheduler, Logger logger)
            : base($"{typeof(StateMachine<TState>).Name}.{nameof(source.StateEntered)}<{typeof(StateEnteredEventArgs<TState>).Name}>", source, tripScheduler, logger)
        {
            if (tripOnState.HasValue)
            {
                _state = source[tripOnState.Value];
            }

            _filterState = tripOnState;
        }

        public StateEnteredTrigger(StateMachine<TState> source, IScheduler tripScheduler, Logger logger)
            : this(source, null, tripScheduler, logger)
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
                StateMachine.StateEntered += HandleSourceStateChanged;
            }
            else
            {
                _state.Entered += HandleStateEntered;
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
                _state.Entered -= HandleStateEntered;
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
