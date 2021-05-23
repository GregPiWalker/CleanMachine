using CleanMachine.Interfaces;
using NodaTime;
using System;
using Unity;

namespace CleanMachine.Behavioral.Behaviors
{
    public class Behavior : IBehavior
    {
        protected Action<IUnityContainer> _action;

        public Behavior(string name, Action<IUnityContainer> action)
        {
            _action = action;
            Name = name;
        }

        public event EventHandler<ClockedEventArgs> Finished;
        public event EventHandler<FaultedEventArgs> Faulted;

        public virtual string Description => "nothing yet";

        public string Name { get; protected set; }

        public virtual void Invoke(IUnityContainer runtimeContainer)
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
        }

        /// <summary>
        /// Raise the Finished event.
        /// </summary>
        protected void OnExecutableFinished(IClock clock)
        {
            try
            {
                Finished?.Invoke(this, new ClockedEventArgs(clock));
            }
            catch (Exception ex)
            {
                //.Log(LogType, LogMessageType.Error, GetType().ToString(), "Executable '" + Name + "' encountered exception while raising Finished event: " + e.Message, e);
            }
        }

        /// <summary>
        /// Raise the Faulted event.
        /// </summary>
        protected virtual void OnExecutableFaulted(Exception ex, IClock clock)
        {
            try
            {
                Faulted?.Invoke(this, new FaultedEventArgs(ex, clock));
            }
            catch (Exception nex)
            {
                //.Log(LogType, LogMessageType.Error, GetType().ToString(), "Executable '" + Name + "' encountered exception while raising Faulted event: " + ex.Message, ex);
            }
        }
    }
}
