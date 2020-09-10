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
    }
}
