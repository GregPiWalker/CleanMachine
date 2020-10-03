using System;
using System.ComponentModel;
using System.Collections.Specialized;
using log4net;
using CleanMachine.Generic;

namespace CleanMachine
{
    public class TransitionEditor
    {
        protected readonly Transition _transition;
        protected readonly ILog _logger;

        internal TransitionEditor(Transition transition, ILog logger)
        {
            _transition = transition;
            _logger = logger;
        }

        internal protected ILog Logger => _logger;

        internal protected Transition Transition => _transition;

        public TransitionEditor GuardWith(Func<bool> condition, string name)
        {
            _transition.Guard = new Constraint(name, condition, _logger);
            return this;
        }

        public TransitionEditor EffectOnSuccess(EventHandler<Interfaces.TransitionEventArgs> successEffect)
        {
            _transition.Succeeded += successEffect;
            return this;
        }

        /// <summary>
        /// Set the Guard to a condition that the incoming signal matches the desiredSignal.
        /// </summary>
        /// <param name="desiredSignal"></param>
        /// <param name="guardName"></param>
        /// <returns></returns>
        public TransitionEditor GuardWithSignalCondition(string desiredSignal, string guardName)
        {
            _transition.Guard = new Constraint<SignalEventArgs>(guardName, (s) => s.Signal == desiredSignal, _logger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName) where TFilterArgs : EventArgs
        {
            var trigger = new Trigger<TSource, TFilterArgs>(source, eventName, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TDelegate, TFilterArgs>(TSource source, string eventName) where TFilterArgs : EventArgs
        {
            var trigger = new DelegateTrigger<TSource, TDelegate, TFilterArgs>(source, eventName, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName, Constraint<TFilterArgs> filter) where TFilterArgs : EventArgs
        {
            var trigger = new Trigger<TSource, TFilterArgs>(source, eventName, filter, _logger);
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
    }
}
