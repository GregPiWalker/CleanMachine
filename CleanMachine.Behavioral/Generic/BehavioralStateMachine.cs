using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Unity;
using CleanMachine.Generic;

namespace CleanMachine.Behavioral.Generic
{
    /// <summary>
    /// For now, this class just exists to satisfy a project dependency.
    /// TODO:  Refactor StateMachine<>, and State<>, and eliminate Behavioral namespace.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public sealed class BehavioralStateMachine<TState> : StateMachine<TState> where TState : struct
    {
        public BehavioralStateMachine(string name, ILog logger)
            : base(name, logger)
        {
        }

        public BehavioralStateMachine(string name, ILog logger, bool createStates) 
            : base(name, logger, createStates)
        {
        }

        public BehavioralStateMachine(string name, IUnityContainer runtimeContainer, ILog logger, bool createStates)
            : base(name, runtimeContainer, logger, createStates)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateNames"></param>
        protected override void CreateStates(IEnumerable<string> stateNames)
        {
            if (!stateNames.Any(name => name.Equals(RequiredCommonStateValue)))
            {
                throw new InvalidOperationException($"StateMachine requires a state enum that contains the value {RequiredCommonStateValue}.");
            }

            foreach (var stateName in stateNames)
            {
                var state = new BehavioralState(stateName, Name, RuntimeContainer, Logger);
                _states.Add(state);
                state.EnteredInternal += HandleStateEnteredInternal;
                state.Entered += HandleStateEntered;
                state.Exited += HandleStateExited;
            }
        }
    }
}