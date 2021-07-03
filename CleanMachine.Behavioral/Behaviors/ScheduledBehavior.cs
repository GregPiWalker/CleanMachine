using NodaTime;
using Unity;
using log4net;
using System;
using System.Reactive.Concurrency;

namespace CleanMachine.Behavioral.Behaviors
{
    /// <summary>
    /// A <see cref="Behavior"/> implementation that schedules its action on
    /// an <see cref="IScheduler"/> for asynchronous execution.
    /// </summary>
    public class ScheduledBehavior : Behavior
    {
        protected readonly IScheduler _scheduler;

        public ScheduledBehavior(string name, Action<IUnityContainer> action, IScheduler scheduler)
            : base(name, action)
        {
            _scheduler = scheduler;
        }

        public override void Invoke(IUnityContainer runtimeContainer)
        {
            _scheduler.Schedule(runtimeContainer, (_, t) =>
            {
                IClock clock = null;
                var logger = runtimeContainer.TryGetTypeRegistration<ILog>();
                try
                {
                    clock = runtimeContainer.Resolve<IClock>();
                    _action(runtimeContainer);
                    OnExecutableFinished(runtimeContainer);
                }
                catch (Exception ex)
                {
                    //TODO: attach the exception to the runtime container somehow
                    Fault = ex;
                    OnExecutableFaulted(ex, runtimeContainer);
                }
            });
        }
    }
}
