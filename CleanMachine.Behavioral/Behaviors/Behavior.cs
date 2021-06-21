using CleanMachine.Interfaces;
using log4net;
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

        public virtual string Description => "REPLACE ME";

        public string Name { get; protected set; }

        public Exception Fault { get; protected set; }

        public virtual void Invoke(IUnityContainer runtimeContainer)
        {
            var clock = runtimeContainer.TryGetTypeRegistration<IClock>();
            var logger = runtimeContainer.TryGetTypeRegistration<ILog>();
            try
            {
                _action(runtimeContainer);
                OnExecutableFinished(clock, logger);
            }
            catch (Exception e)
            {
                Fault = e;
                OnExecutableFaulted(e, clock, logger);
            }
        }

        /// <summary>
        /// Raise the Finished event.
        /// </summary>
        protected void OnExecutableFinished(IClock clock, ILog logger)
        {
            try
            {
                Finished?.Invoke(this, new ClockedEventArgs(clock));
            }
            catch (Exception ex)
            {
                //TODO: what if the logger is null?
                logger?.Error($"{ex.GetType().Name} while raising event '{nameof(Finished)}' in behavior '{Name}':  {ex.Message}.", ex);
            }
        }

        /// <summary>
        /// Raise the Faulted event.
        /// </summary>
        protected virtual void OnExecutableFaulted(Exception ex, IClock clock, ILog logger)
        {
            try
            {
                Faulted?.Invoke(this, new FaultedEventArgs(ex, clock));
            }
            catch (Exception nex)
            {
                //TODO: what if the logger is null?
                logger?.Error($"{nex.GetType().Name} while raising event '{nameof(Faulted)}' in behavior '{Name}':  {nex.Message}.", nex);
            }
        }
    }
}
