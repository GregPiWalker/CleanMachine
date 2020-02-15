using log4net;
using CleanMachine.Generic;

namespace CleanMachine
{
    public class StateBuilder<TState> where TState : struct
    {
        private readonly StateMachine<TState> _machine;
        private readonly TState _state;

        internal StateBuilder(StateMachine<TState> machine, TState state)
        {
            _machine = machine;
            _state = state;
        }

        public TransitionBuilder TransitionTo(TState toState)
        {
            var transition = _machine.CreateTransition(_state, toState);
            transition.Edit();
            return new TransitionBuilder(transition, _machine.Logger);
        }
    }
}
