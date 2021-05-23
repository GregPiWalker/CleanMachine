using CleanMachine.Generic;
using log4net;

namespace CleanMachine
{
    public static class StateMachineFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static StateMachine<TState> Create<TState>(string name, ILog logger) where TState : struct
        {
            return new StateMachine<TState>(name, logger);
        }

        /// <summary>
        /// Create a StateMachine that transitions synchronously.  An option is given whether to make the UML behaviors
        /// (ENTRY, DO, EXIT, EFFECT) synchronous or not.  If asynchronous behaviors is chosen, a scheduler with a 
        /// dedicated background thread is instantiated for running them.  This optional scheduler serializes its workflow,
        /// but will operate asynchronously with respect to transitions and incoming trigger invocations.
        /// If synchronous behaviors is chosen, then transitions, behaviors and trigger invocations will all occur
        /// on the current thread.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="signalSynchronizer"></param>
        /// <returns></returns>
        public static StateMachine<TState> Create<TState>(string name, ILog logger, object signalSynchronizer) where TState : struct
        {
            return new StateMachine<TState>(name, null, logger, false, signalSynchronizer);
        }
    }
}
