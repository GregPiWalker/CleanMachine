using log4net;
using CleanMachine.Generic;
using System.Reactive.Concurrency;
using System.Threading;
using Unity.Lifetime;
using Unity;
using CleanMachine.Behavioral.Generic;

namespace CleanMachine.Behavioral
{
    public static class StateMachineFactory
    {
        /// <summary>
        /// Create a fully asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// internal transitions.  Another scheduler with a dedicated background thread is instantiated for running
        /// the following behaviors: ENTRY, DO, EXIT, EFFECT.  Both schedulers serialize their workflow, but will
        /// operate asynchronously with respect to each other, as well as with respect to incoming trigger invocations.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreateAsync<TState>(string name, ILog logger) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var triggerScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{name} Trigger Scheduler", IsBackground = true }; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.TriggerSchedulerKey, triggerScheduler, new ContainerControlledLifetimeManager());
            var behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{name} Behavior Scheduler", IsBackground = true }; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.BehaviorSchedulerKey, behaviorScheduler, new ContainerControlledLifetimeManager());
            var machine = new BehavioralStateMachine<TState>(name, container, logger, true, null);
            return machine;
        }

        /// <summary>
        /// Create a partially asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// internal triggers and signals.  UML behaviors (ENTRY, DO, EXIT, EFFECT) are executed synchronously on the same internal thread.
        /// The scheduler serializes its workflow, but will operate asynchronously with respect to incoming trigger invocations.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="name">The state machine name.</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreateTriggerAsync<TState>(string name, ILog logger) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var triggerScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{name} Trigger Scheduler", IsBackground = true }; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.TriggerSchedulerKey, triggerScheduler, new ContainerControlledLifetimeManager());
            var machine = new BehavioralStateMachine<TState>(name, container, logger, true, null);
            return machine;
        }

        /// <summary>
        /// Create a partially asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// UML behaviors (ENTRY, DO, EXIT, EFFECT).  Internal triggers and signals are executed synchronously on the same thread
        /// as the triggers' event handlers.
        /// The scheduler serializes its workflow, but will operate asynchronously with respect to incoming trigger invocations.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="externalSynchronizer">An optional object to synchronize the state machine's internal triggers and signals with other external threaded work.
        /// If none is supplied, an internal object is used.</param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreateBehaviorAsync<TState>(string name, ILog logger, object externalSynchronizer = null) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{name} Behavior Scheduler", IsBackground = true }; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.BehaviorSchedulerKey, behaviorScheduler, new ContainerControlledLifetimeManager());
            var machine = new BehavioralStateMachine<TState>(name, container, logger, true, externalSynchronizer);
            return machine;
        }
    }
}
