using log4net;
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
        /// Both schedulers' life-cycles are managed by the Unity container, so disposal is automatic.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="machineName"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreateAsync<TState>(string machineName, ILog logger) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var triggerScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{machineName} Trigger Scheduler", IsBackground = true }; });
            // Scheduling this dummy operation forces the underlying thread to be instantiated now.
            triggerScheduler.Schedule(() => { bool dummy = true; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.TriggerSchedulerKey, triggerScheduler, new ContainerControlledLifetimeManager());
            var behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{machineName} Behavior Scheduler", IsBackground = true }; });
            // Scheduling this dummy operation forces the underlying thread to be instantiated now.
            behaviorScheduler.Schedule(() => { bool dummy = true; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.BehaviorSchedulerKey, behaviorScheduler, new ContainerControlledLifetimeManager());
            var machine = new BehavioralStateMachine<TState>(machineName, container, logger, true);
            return machine;
        }

        /// <summary>
        /// Create a partially asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// internal triggers and signals.  UML behaviors (ENTRY, DO, EXIT, EFFECT) are executed synchronously on the same internal thread.
        /// The scheduler serializes its workflow, but will operate asynchronously with respect to incoming trigger invocations.
        /// The scheduler's life-cycle is managed by the Unity container, so disposal is automatic.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="machineName">The state machine name.</param>
        /// <param name="logger"></param>
        /// <param name="externalSynchronizer">An optional object to synchronize the state machine's internal triggers and signals with other external threaded work.
        /// If none is supplied, an internal object is used.</param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreateTriggerAsync<TState>(string machineName, ILog logger, object externalSynchronizer = null) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var triggerScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{machineName} Trigger Scheduler", IsBackground = true }; });
            // Scheduling this dummy operation forces the underlying thread to be instantiated now.
            triggerScheduler.Schedule(() => { bool dummy = true; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.TriggerSchedulerKey, triggerScheduler, new ContainerControlledLifetimeManager());
            if (externalSynchronizer != null)
            {
                container.RegisterInstance(StateMachineBase.GlobalSynchronizerKey, externalSynchronizer);
            }

            var machine = new BehavioralStateMachine<TState>(machineName, container, logger, true);
            return machine;
        }

        /// <summary>
        /// Create a partially asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// UML behaviors (ENTRY, DO, EXIT, EFFECT).  Internal triggers and signals are executed synchronously on the same thread
        /// as the triggers' event handlers.
        /// The scheduler serializes its workflow, but will operate asynchronously with respect to incoming trigger invocations.
        /// The scheduler's life-cycle is managed by the Unity container, so disposal is automatic.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="machineName"></param>
        /// <param name="logger"></param>
        /// <param name="externalSynchronizer">An optional object to synchronize the state machine's internal triggers and signals with other external threaded work.
        /// If none is supplied, an internal object is used.</param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreateBehaviorAsync<TState>(string machineName, ILog logger, object externalSynchronizer = null) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{machineName} Behavior Scheduler", IsBackground = true }; });
            // Scheduling this dummy operation forces the underlying thread to be instantiated now.
            behaviorScheduler.Schedule(() => { bool dummy = true; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.BehaviorSchedulerKey, behaviorScheduler, new ContainerControlledLifetimeManager());
            if (externalSynchronizer != null)
            {
                container.RegisterInstance(StateMachineBase.GlobalSynchronizerKey, externalSynchronizer);
            }

            var machine = new BehavioralStateMachine<TState>(machineName, container, logger, true);
            return machine;
        }
    }
}
