using CleanMachine.Generic;

namespace CleanMachine
{
    //public class StateEditor<TState> where TState : struct
    //{
    //    private readonly StateMachine<TState> _machine;
    //    private readonly TState _state;

    //    internal StateEditor(StateMachine<TState> machine, TState state)
    //    {
    //        _machine = machine;
    //        _state = state;
    //    }

    //    public TransitionEditor TransitionTo(TState toState)
    //    {
    //        var transition = _machine.CreateTransition(_state, toState);
    //        transition.Edit();
    //        return new TransitionEditor(transition, _machine.Logger);
    //    }
    //}
}
