using log4net;
using CleanMachine.Generic;
using System.Reactive.Concurrency;
using System.Threading;
using Unity.Lifetime;
using Unity;

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
        public static StateMachine<TState> CreateAsync<TState>(string name, ILog logger) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var triggerScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{name} Trigger Scheduler", IsBackground = true }; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.TriggerSchedulerKey, triggerScheduler, new ContainerControlledLifetimeManager());
            var behaviorScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{name} Behavior Scheduler", IsBackground = true }; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.BehaviorSchedulerKey, behaviorScheduler, new ContainerControlledLifetimeManager());
            var machine = new StateMachine<TState>(name, container, logger, true, null);
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
        /// <param name="synchronizer">an optional object to synchronize the state machine internally and externally.</param>
        /// <returns></returns>
        public static StateMachine<TState> CreatePartialAsync<TState>(string name, ILog logger, object synchronizer) where TState : struct
        {
            IUnityContainer container = new UnityContainer();
            var triggerScheduler = new EventLoopScheduler((a) => { return new Thread(a) { Name = $"{name} Trigger Scheduler", IsBackground = true }; });
            container.RegisterInstance(typeof(IScheduler), StateMachineBase.TriggerSchedulerKey, triggerScheduler, new ContainerControlledLifetimeManager());
            var machine = new StateMachine<TState>(name, container, logger, true, synchronizer);
            return machine;
        }
    }
}
