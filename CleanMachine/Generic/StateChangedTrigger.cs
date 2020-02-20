﻿
using log4net;
using System;
using System.Collections.Generic;

namespace CleanMachine.Generic
{
    /// <summary>
    /// This class is a convenience for creating a trigger that listens for the StateChanged event from 
    /// one or more <see cref="StateMachine{TState}"/>s.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class StateChangedTrigger<TState> : TriggerBase where TState : struct
    {
        private readonly TState? _filterState;

        public StateChangedTrigger(List<StateMachine<TState>> source, TState? tripOnState, ILog logger)
            : base($"{typeof(StateMachine<TState>).Name}.StateChanged<{typeof(StateChangedEventArgs<TState>).Name}>", source, logger)
        {
            _filterState = tripOnState;
        }

        public StateChangedTrigger(StateMachine<TState> source, TState? tripOnState, ILog logger)
            : this(new List<StateMachine<TState>>() { source }, tripOnState, logger)
        {
        }

        public StateChangedTrigger(StateMachine<TState> source, ILog logger)
            : this(source, null, logger)
        {
        }

        /// <summary>
        /// Gets the type cast base source.
        /// </summary>
        private List<StateMachine<TState>> SourceMachines
        {
            get { return Source as List<StateMachine<TState>>; }
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
            if (!_filterState.HasValue || args.CurrentState.Equals(_filterState.Value))
            {
                Trip(sender, args);
            }
        }
    }
}
