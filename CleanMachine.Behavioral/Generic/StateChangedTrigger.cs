
using log4net;
using System;
using System.Collections.Generic;

namespace CleanMachine.Behavioral.Generic
{
    /// <summary>
    /// This class is a convenience for creating a trigger that listens for the StateChanged event from 
    /// one or more <see cref="BehavioralStateMachine{TState}"/>s.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class StateChangedTrigger<TState> : TriggerBase where TState : struct
    {
        private readonly TState? _filterState;

        public StateChangedTrigger(List<BehavioralStateMachine<TState>> source, TState? tripOnState, ILog logger)
            : base($"{typeof(BehavioralStateMachine<TState>).Name}.StateChanged<{typeof(StateChangedEventArgs<TState>).Name}>", source, logger)
        {
            _filterState = tripOnState;
        }

        public StateChangedTrigger(BehavioralStateMachine<TState> source, TState? tripOnState, ILog logger)
            : this(new List<BehavioralStateMachine<TState>>() { source }, tripOnState, logger)
        {
        }

        public StateChangedTrigger(BehavioralStateMachine<TState> source, ILog logger)
            : this(source, null, logger)
        {
        }

        /// <summary>
        /// Gets the type cast base source.
        /// </summary>
        private List<BehavioralStateMachine<TState>> SourceMachines
        {
            get { return Source as List<BehavioralStateMachine<TState>>; }
        }

        public override string ToString()
        {
            return _filterState.HasValue ? $"{Name}[State=={_filterState.Value.ToString()}]" : Name;
        }

        protected override void Enable()
        {
            SourceMachines.ForEach(m => m.StateChanged += HandleSourceStateChanged);
        }

        protected override void Disable()
        {
            SourceMachines.ForEach(m => m.StateChanged -= HandleSourceStateChanged);
        }

        private void HandleSourceStateChanged(object sender, StateChangedEventArgs<TState> args)
        {
            if (!_filterState.HasValue || args.ResultingState.Equals(_filterState.Value))
            {
                Trip(sender, args);
            }
        }
    }
}
