
using CleanMachine.Interfaces.Generic;

namespace CleanMachine.Generic
{
    /// <summary>
    /// TODO: REFACTOR
    /// </summary>
    public class StateEditor<TState> where TState : struct
    {
        internal StateEditor(IStateMachine<TState> machine, TState state)
        {
            Machine = machine as StateMachine<TState>;
            State = state;
        }

        internal protected StateMachine<TState> Machine { get; }

        internal protected TState State { get; }

        public TransitionEditor TransitionTo(TState toState)
        {
            var transition = Machine.CreateTransition(State, toState);
            transition.Edit();
            return new TransitionEditor(transition, Machine.TriggerScheduler, Machine.Logger);
        }
    }
}
