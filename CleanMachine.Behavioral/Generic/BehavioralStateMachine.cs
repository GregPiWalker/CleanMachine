using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
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

        public new BehavioralState this[TState value]
        {
            get { return FindState(value) as BehavioralState; }
        }

        public void AddDoBehavior(TState state, Action<IUnityContainer> doAction, string doName = null)
        {
            var stateObj = this[state];
            var name = doName ?? $"{BehavioralState.DoBehaviorName} {stateObj.DoBehaviorCount + 1}";
            if (stateObj.Editable)
            {
                stateObj.AddDoBehavior(name, doAction);
            }
            else if (RuntimeContainer.IsRegistered<object>(GlobalSynchronizerKey))
            {
                var synchronizer = RuntimeContainer.TryGetInstance<object>(GlobalSynchronizerKey);
                lock (synchronizer)
                {
                    stateObj.AddDoBehaviorUnsafe(stateObj.CreateBehavior(name, doAction));
                }
            }
            else if (BehaviorScheduler != null)
            {
                BehaviorScheduler.Schedule(() => { stateObj.AddDoBehaviorUnsafe(stateObj.CreateBehavior(name, doAction)); });
            }
            else
            {
                throw new InvalidOperationException($"{Name}: State '{stateObj.Name}' must be editable in order to add a DO behavior.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateNames"></param>
        protected override void CreateStates(IEnumerable<string> stateNames)
        {
            if (!stateNames.Any(name => name.Equals(RequiredCommonStateValue)))
            {
                throw new InvalidOperationException($"{Name}:  StateMachine requires a state enum that contains the value {RequiredCommonStateValue}.");
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