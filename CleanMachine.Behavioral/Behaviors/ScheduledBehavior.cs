using NodaTime;
using Unity;
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
                var clock = runtimeContainer.Resolve<IClock>();
                try
                {
                    _action(runtimeContainer);
                    OnExecutableFinished(clock);
                }
                catch (Exception e)
                {
                    OnExecutableFaulted(e, clock);
                }
            });
        }
    }
}
