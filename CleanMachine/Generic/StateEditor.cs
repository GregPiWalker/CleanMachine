
namespace CleanMachine.Generic
{
    /// <summary>
    /// TODO: REFACTOR
    /// </summary>
    public class StateEditor<TState> where TState : struct
    {
        internal StateEditor(StateMachine<TState> machine, TState state)
        {
            Machine = machine;
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
