using CleanMachine.Interfaces;
using log4net;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace CleanMachine
{
    //TODO: finish
    ///// <summary>
    ///// This class is a convenience for creating a trigger that listens for the StateChanged event from 
    ///// one or more <see cref="StateMachineBase"/>s.
    ///// </summary>
    ///// <typeparam name="TState"></typeparam>
    //public class StateChangedTrigger : TriggerBase
    //{
    //    private readonly IState? _filterState;

    //    public StateChangedTrigger(List<StateMachineBase> source, IState? tripOnState, IScheduler tripScheduler, ILog logger)
    //        : base($"{typeof(StateMachineBase).Name}.StateChanged<{typeof(StateChangedEventArgs).Name}>", source, tripScheduler, logger)
    //    {
    //        _filterState = tripOnState;
    //    }

    //    public StateChangedTrigger(StateMachineBase source, IState? tripOnState, IScheduler tripScheduler, ILog logger)
    //        : this(new List<StateMachineBase>() { source }, tripOnState, tripScheduler, logger)
    //    {
    //    }

    //    public StateChangedTrigger(StateMachineBase source, IScheduler tripScheduler, ILog logger)
    //        : this(source, null, tripScheduler, logger)
    //    {
    //    }

    //    /// <summary>
    //    /// Gets the type cast base source.
    //    /// </summary>
    //    private List<StateMachineBase> SourceMachines
    //    {
    //        get { return Source as List<StateMachineBase>; }
    //    }

    //    public override string ToString()
    //    {
    //        return _filterState.HasValue ? $"{Name}[State=={_filterState.Value.ToString()}]" : Name;
    //    }

    //    protected override void Enable()
    //    {
    //        SourceMachines.ForEach(m => m. += HandleSourceStateChanged);
    //    }

    //    protected override void Disable()
    //    {
    //        SourceMachines.ForEach(m => m.StateChanged -= HandleSourceStateChanged);
    //    }

    //    private void HandleSourceStateChanged(object sender, StateChangedEventArgs args)
    //    {
    //        if (!_filterState.HasValue || args.ResultingState.Equals(_filterState.Value))
    //        {
    //            Trip(sender, args);
    //        }
    //    }
    //}
}
