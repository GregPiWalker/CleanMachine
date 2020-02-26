using CleanMachine.Generic;
using log4net;

namespace CleanMachine
{
    public static class StateMachineFactory
    {
        /// <summary>
        /// Create a fully asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// internal transitions.  Another scheduler with a dedicated background thread is instantiated for running
        /// the following behaviors: ENTRY, DO, EXIT, EFFECT.  Both schedulers serialize their workflow, but will
        /// operate asynchronously with respect to each other, as well as with respect to incoming trigger invocations.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreateAsync<TState>(string name, ILog logger) where TState : struct
        {
            return new BehavioralStateMachine<TState>(name, logger, null, true, true);
        }

        /// <summary>
        /// Create a partially asynchronous StateMachine.  A scheduler with a dedicated background thread is instantiated for
        /// internal transitions.  UML behaviors (ENTRY, DO, EXIT, EFFECT) are executed synchronously on the same transition thread.
        /// The scheduler serializes its workflow, but will operate asynchronously with respect to incoming trigger invocations.
        /// This configuration gives you an option of supplying a global synchronization context that can be used to synchronize
        /// transitions (state changes) across multiple state machines.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="globalSyncContext">If global transition synchronization across multiple <see cref="StateMachine"/>s is desired,
        /// supply a synchronization context here. Otherwise, supply a null value.</param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> CreatePartialAsync<TState>(string name, ILog logger, object globalSyncContext = null) where TState : struct
        {
            return new BehavioralStateMachine<TState>(name, logger, globalSyncContext, true, false);
        }

        /// <summary>
        /// Create a StateMachine that transitions synchronously.  An option is given whether to make the UML behaviors
        /// (ENTRY, DO, EXIT, EFFECT) synchronous or not.  If asynchronous behaviors is chosen, a scheduler with a 
        /// dedicated background thread is instantiated for running them.  This optional scheduler serializes its workflow,
        /// but will operate asynchronously with respect to transitions and incoming trigger invocations.
        /// If synchronous behaviors is chosen, then transitions, behaviors and trigger invocations will all occur
        /// on the current thread.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="logger"></param>
        /// <param name="asynchronousBehaviors"></param>
        /// <returns></returns>
        public static BehavioralStateMachine<TState> Create<TState>(string name, ILog logger, bool asynchronousBehaviors) where TState : struct
        {
            return new BehavioralStateMachine<TState>(name, logger, null, false, asynchronousBehaviors);
        }

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
    }
}
