
using log4net;
using CleanMachine.Interfaces;
using System;

namespace CleanMachine.Generic
{
    /// <summary>
    /// This class is a convenience for creating a trigger that listens for the StateChanged event from a state machine.
    /// 
    /// NOTE: This is meant to be used for one StateMachine to observe another.
    /// It is not recommended to use this trigger to observe the same StateMachine
    /// in which it is defined.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class StateChangedTrigger<TState> : TriggerBase where TState : struct
    {
        private readonly TState? _filterState;

        public StateChangedTrigger(StateMachine<TState> source, TState? tripOnState, ILog logger)
            : base($"{typeof(StateMachine<TState>).Name}.{nameof(source.StateChanged)}<{typeof(StateChangedEventArgs<TState>).Name}>", source, logger)
        {
            _filterState = tripOnState;
        }

        public StateChangedTrigger(StateMachine<TState> source, ILog logger)
            : this(source, null, logger)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override Type SourceType => typeof(StateChangedTrigger<TState>);

        /// <summary>
        /// Gets the shadow implementation of the base source.
        /// </summary>
        public StateMachine<TState> Machine
        {
            get { return Source as StateMachine<TState>; }
        }

        public override string ToString()
        {
            return _filterState.HasValue ? $"{Name}[State=={_filterState.Value.ToString()}]" : Name;
        }

        protected override void Enable()
        {
            Machine.StateChanged += HandleSourceStateChanged;
        }

        protected override void Disable()
        {
            Machine.StateChanged -= HandleSourceStateChanged;
        }

        private void HandleSourceStateChanged(object sender, StateChangedEventArgs<TState> args)
        {
            if (!_filterState.HasValue || args.CurrentState.Equals(_filterState.Value))
            {
                Trip(sender, args);
            }
        }
    }
}
