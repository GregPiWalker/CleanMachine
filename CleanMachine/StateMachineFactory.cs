using CleanMachine.Generic;
using log4net;
using Unity;

namespace CleanMachine
{
    public static class StateMachineFactory
    {
        /// <summary>
        /// Create a StateMachine that transits synchronously.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="externalSynchronizer">An object to synchronize the state machine's internal triggers and signals with other external threaded work.
        /// If none is supplied, an internal object is used.</param>
        /// <returns></returns>
        public static StateMachine<TState> Create<TState>(string name, ILog logger, object externalSynchronizer = null) where TState : struct
        {
            IUnityContainer container = null;
            if (externalSynchronizer != null)
            {
                container = new UnityContainer();
                container.RegisterInstance(StateMachineBase.GlobalSynchronizerKey, externalSynchronizer);
            }

            return new StateMachine<TState>(name, container, logger, true);
        }
    }
}
