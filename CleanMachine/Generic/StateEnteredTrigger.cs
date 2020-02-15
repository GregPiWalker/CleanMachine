using log4net;
using CleanMachine.Interfaces;
using System;

namespace CleanMachine.Generic
{
    /// <summary>
    /// A trigger that listens for the StateEntered event from a state machine.
    /// StateEntered is raised before the state's Enter & Do behaviors are done.
    /// 
    /// NOTE: This is meant to be used for one StateMachine to observe another.
    /// It is not recommended to use this trigger to observe the same StateMachine
    /// in which it is defined.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class StateEnteredTrigger<TState> : TriggerBase where TState : struct
    {
        private readonly TState? _filterState;

        public StateEnteredTrigger(StateMachine<TState> source, TState? tripOnState, ILog logger)
            : base($"{typeof(StateMachine<TState>).Name}.{nameof(source.StateEntered)}<{typeof(StateEnteredEventArgs<TState>).Name}>", source, logger)
        {
            _filterState = tripOnState;
        }

        public StateEnteredTrigger(string sourceName, StateMachine<TState> source, ILog logger)
            : this(source, null, logger)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override Type SourceType => typeof(StateEnteredTrigger<TState>);

        public StateMachine<TState> StateMachine
        {
            get { return base.Source as StateMachine<TState>; }
        }

        protected override void Enable()
        {
            StateMachine.StateEntered += HandleSourceStateChanged;
        }

        protected override void Disable()
        {
            StateMachine.StateEntered -= HandleSourceStateChanged;
        }

        private void HandleSourceStateChanged(object sender, StateEnteredEventArgs<TState> args)
        {
            if (!_filterState.HasValue || args.State.Equals(_filterState.Value))
            {
                Trip(sender, args);
            }
        }
    }
}
