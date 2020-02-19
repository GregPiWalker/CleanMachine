
using log4net;
using System;

namespace CleanMachine.Generic
{
    /// <summary>
    /// This class is a convenience for creating a trigger that listens for the StateChanged event from a state machine.
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
