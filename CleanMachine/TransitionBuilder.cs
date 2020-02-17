using System;
using System.ComponentModel;
using CleanMachine.Generic;
using System.Linq.Expressions;
using System.Collections.Specialized;
using log4net;

namespace CleanMachine
{
    public class TransitionEditor
    {
        private readonly Transition _transition;
        private readonly ILog _logger;

        internal TransitionEditor(Transition transition, ILog logger)
        {
            _transition = transition;
            _logger = logger;
        }

        public TransitionEditor GuardWith(Func<bool> condition, string name)
        {
            _transition.Guard = new Constraint(name, condition, _logger);
            return this;
        }

        public TransitionEditor HaveEffect(Action effect)
        {
            _transition.Effect = effect;
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName) where TFilterArgs : EventArgs
        {
            Trigger<TSource, TFilterArgs> trigger = new Trigger<TSource, TFilterArgs>(source, eventName, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName, Constraint<TFilterArgs> filter) where TFilterArgs : EventArgs
        {
            Trigger<TSource, TFilterArgs> trigger = new Trigger<TSource, TFilterArgs>(source, eventName, filter, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, Expression<Func<TSource, EventHandler<TFilterArgs>>> eventExpression, Constraint<TFilterArgs> filter) where TFilterArgs : EventArgs
        {
            Trigger<TSource, TFilterArgs> trigger = new Trigger<TSource, TFilterArgs>(source, ((MemberExpression)eventExpression.Body).Member.Name, filter, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithTransition<TState>(StateMachine<TState> machine, TState toState) where TState : struct
        {
            var trigger = new StateChangedTrigger<TState>(machine, toState, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithProperty(INotifyPropertyChanged sender, string propertyNameChain = "")
        {
            var trigger = new PropertyChangedTrigger(sender, propertyNameChain, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithCollection(INotifyCollectionChanged sender, int tripOnCount = -1)
        {
            var trigger = new CollectionChangedTrigger(sender, tripOnCount, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        //public TransitionBuilder TriggerWithProperties<TSender>(IEnumerable<TSender> sender, string propertyName = "") where TSender : INotifyPropertyChanged
        //{
        //    var trigger = new MultiPropertyChangedTrigger(sender, propertyName, logger);
        //    _transition.AddTrigger(trigger);
        //    return this;
        //}
    }
}
