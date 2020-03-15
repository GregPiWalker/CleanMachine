using System;
using System.ComponentModel;
using CleanMachine.Generic;
using System.Collections.Specialized;
using log4net;
using System.Collections.Generic;

namespace CleanMachine
{
    //public class TransitionEditor
    //{
    //    private readonly Transition _transition;
    //    private readonly ILog _logger;

    //    internal TransitionEditor(Transition transition, ILog logger)
    //    {
    //        _transition = transition;
    //        _logger = logger;
    //    }

    //    public TransitionEditor GuardWith(Func<bool> condition, string name)
    //    {
    //        _transition.Guard = new Constraint(name, condition, _logger);
    //        return this;
    //    }

    //    public TransitionEditor HaveEffect(Action effect)
    //    {
    //        var transition = _transition as BehavioralTransition;
    //        if (transition == null)
    //        {
    //            throw new InvalidOperationException($"");
    //        }

    //        transition.Effect = effect;
    //        return this;
    //    }

    //    public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName) where TFilterArgs : EventArgs
    //    {
    //        var trigger = new Trigger<TSource, TFilterArgs>(source, eventName, _logger);
    //        _transition.AddTrigger(trigger);
    //        return this;
    //    }

    //    public TransitionEditor TriggerWithEvent<TSource, TDelegate, TFilterArgs>(TSource source, string eventName) where TFilterArgs : EventArgs
    //    {
    //        var trigger = new DelegateTrigger<TSource, TDelegate, TFilterArgs>(source, eventName, _logger);
    //        _transition.AddTrigger(trigger);
    //        return this;
    //    }

    //    public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName, Constraint<TFilterArgs> filter) where TFilterArgs : EventArgs
    //    {
    //        var trigger = new Trigger<TSource, TFilterArgs>(source, eventName, filter, _logger);
    //        _transition.AddTrigger(trigger);
    //        return this;
    //    }

    //    //public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, Expression<Func<TSource, EventHandler<TFilterArgs>>> eventExpression, Constraint<TFilterArgs> filter) where TFilterArgs : EventArgs
    //    //{
    //    //    var trigger = new Trigger<TSource, TFilterArgs>(source, ((MemberExpression)eventExpression.Body).Member.Name, filter, _logger);
    //    //    _transition.AddTrigger(trigger);
    //    //    return this;
    //    //}

    //    public TransitionEditor TriggerWithStateChange<TState>(BehavioralStateMachine<TState> machine, TState toState) where TState : struct
    //    {
    //        var trigger = new StateChangedTrigger<TState>(machine, toState, _logger);
    //        _transition.AddTrigger(trigger);
    //        return this;
    //    }

    //    public TransitionEditor TriggerWithStateChange<TState>(List<BehavioralStateMachine<TState>> machines, TState toState) where TState : struct
    //    {
    //        var trigger = new StateChangedTrigger<TState>(machines, toState, _logger);
    //        _transition.AddTrigger(trigger);
    //        return this;
    //    }

    //    public TransitionEditor TriggerWithProperty(INotifyPropertyChanged sender, string propertyNameChain = "")
    //    {
    //        var trigger = new PropertyChangedTrigger(sender, propertyNameChain, _logger);
    //        _transition.AddTrigger(trigger);
    //        return this;
    //    }

    //    public TransitionEditor TriggerWithCollection(INotifyCollectionChanged sender, int tripOnCount = -1)
    //    {
    //        var trigger = new CollectionChangedTrigger(sender, tripOnCount, _logger);
    //        _transition.AddTrigger(trigger);
    //        return this;
    //    }

    //    //public TransitionBuilder TriggerWithProperties<TSender>(IEnumerable<TSender> sender, string propertyName = "") where TSender : INotifyPropertyChanged
    //    //{
    //    //    var trigger = new GroupPropertyChangedTrigger(sender, propertyName, logger);
    //    //    _transition.AddTrigger(trigger);
    //    //    return this;
    //    //}
    //}
}
