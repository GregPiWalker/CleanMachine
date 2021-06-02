using CleanMachine;
using CleanMachine.Behavioral;
using CleanMachine.Generic;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Reactive.Concurrency;
using Unity;
using log4net;

namespace Activity
{
    public abstract class ActivityBuilderBase
    {
        protected readonly ILog _logger;
        protected List<Binder> _linkBinders = new List<Binder>();
        protected Dictionary<Guid, ActionNode> _nodes = new Dictionary<Guid, ActionNode>();

        protected ActivityBuilderBase(ILog logger)
        {
            _logger = logger;
        }

        protected ActivitySequence Output { get; set; }

        /// <summary>
        /// Gets the
        /// This is abstract so that derived types can have a static set of constructors scoped to them specifically.
        /// </summary>
        protected abstract Dictionary<string, Func<TriggerBase>> Stimulus { get; }

        public virtual ActivityBuilderBase Build(string activityName, IScheduler signalScheduler, IScheduler invocationScheduler)
        {
            if (signalScheduler == null)
            {
                signalScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{activityName} Signal Scheduler", IsBackground = true }; });
            }

            if (invocationScheduler == null)
            {
                invocationScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{activityName} Invocation Scheduler", IsBackground = true }; });
            }

            Output = new ActivitySequence(activityName, signalScheduler, invocationScheduler);
            return this;
        }

        protected virtual void ConfigureStimuli()
        {
            //add trigger creators
        }

        protected ActivityBuilderBase Start(string actionName, Action<IUnityContainer> action)
        {
            Output.StartWithBehavior(new Behavior(actionName, action));
            return this;
        }

        public ActivityBuilderBase StartWhen(string conditionName, Func<bool> condition, params string[] reflexKeys)
        {
            var triggers = from key in reflexKeys
                           where Stimulus.ContainsKey(key)
                           select Stimulus[key].Invoke();
            Output.StartWithConstraint(new Constraint(conditionName, condition, _logger), triggers);
            return this;
        }

        protected ActivityBuilderBase Do(string actionName, Action<IUnityContainer> action)
        {
            Output.FinishEditWithBehavior(new Behavior(actionName, action));
            return this;
        }

        protected ActivityBuilderBase When(string conditionName, Func<bool> condition, params string[] reflexKeys)
        {
            var triggers = from key in reflexKeys
                           where Stimulus.ContainsKey(key)
                           select Stimulus[key].Invoke();
            Output.EditWithConstraint(conditionName, condition, triggers);
            return this;
        }

        protected ActivityBuilderBase OrWhen(string conditionName, Func<bool> condition, params string[] reflexKeys)
        {
            var triggers = from key in reflexKeys
                           where Stimulus.ContainsKey(key)
                           select Stimulus[key].Invoke();
            Output.EditWithConstraint(conditionName, condition, triggers);
            return this;
        }

        protected static void AddStimulus<TSource, TEventArgs>(Dictionary<string, Func<TriggerBase>> creators, string key, TSource evSource, string evName, ILog logger, IScheduler scheduler = null, Func<TEventArgs, bool> filter = null, string filterName = null) //where TEventArgs : EventArgs
        {
            if (creators.ContainsKey(key))
            {
                return;
            }

            if (filter == null)
            {
                creators[key] = () => new Trigger<TSource, TEventArgs>(evSource, evName, null, scheduler, logger);
            }
            else
            {
                creators[key] = () => new Trigger<TSource, TEventArgs>(evSource, evName, new Constraint<TEventArgs>(filterName, filter, logger), scheduler, logger);
            }
        }

        protected static void AddDelegateStimulus<TSource, TDelegate, TEventArgs>(Dictionary<string, Func<TriggerBase>> creators, string key, TSource evSource, string evName, Func<TEventArgs, bool> filter = null, string filterName = null) //where TEventArgs : EventArgs
        {
            if (creators.ContainsKey(key))
            {
                return;
            }

            if (filter == null)
            {
                creators[key] = () => new DelegateTrigger<TSource, TDelegate, TEventArgs>(evSource, evName, null);
            }
            else
            {
                creators[key] = () => new DelegateTrigger<TSource, TDelegate, TEventArgs>(evSource, evName, new Constraint<TEventArgs>(filterName, filter, null), null);
            }
        }
    }
}
