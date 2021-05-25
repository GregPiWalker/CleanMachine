﻿using System;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Reactive.Concurrency;
using log4net;
using CleanMachine.Generic;
using CleanMachine.Interfaces;

namespace CleanMachine
{
    public class TransitionEditor
    {
        protected readonly Transition _transition;
        protected readonly ILog _logger;
        protected readonly IScheduler _triggerScheduler;

        internal TransitionEditor(Transition transition, IScheduler triggerScheduler, ILog logger)
        {
            _transition = transition;
            _logger = logger;
            _triggerScheduler = triggerScheduler;
        }

        internal protected ILog Logger => _logger;

        internal protected Transition Transition => _transition;

        public TransitionEditor GuardWith(Func<bool> condition, string name)
        {
            _transition.Guard = new Constraint(name, condition, _logger);
            return this;
        }

        public TransitionEditor EffectOnSuccess(EventHandler<TransitionEventArgs> successEffect)
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
            _transition.Guard = new Constraint<TripEventArgs>(guardName, (s) => s.GetTripOrigin().Signal == desiredSignal, _logger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName) where TFilterArgs : EventArgs
        {
            var trigger = new Trigger<TSource, TFilterArgs>(source, eventName, _triggerScheduler, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TDelegate, TFilterArgs>(TSource source, string eventName) where TFilterArgs : EventArgs
        {
            var trigger = new DelegateTrigger<TSource, TDelegate, TFilterArgs>(source, eventName, _triggerScheduler, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithEvent<TSource, TFilterArgs>(TSource source, string eventName, Constraint<TFilterArgs> filter) where TFilterArgs : EventArgs
        {
            var trigger = new Trigger<TSource, TFilterArgs>(source, eventName, filter, _triggerScheduler, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithProperty(INotifyPropertyChanged sender, string propertyNameChain = "")
        {
            var trigger = new PropertyChangedTrigger(sender, propertyNameChain, _triggerScheduler, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }

        public TransitionEditor TriggerWithCollection(INotifyCollectionChanged sender, int tripOnCount = -1)
        {
            var trigger = new CollectionChangedTrigger(sender, tripOnCount, _triggerScheduler, _logger);
            _transition.AddTrigger(trigger);
            return this;
        }
    }
}
