using CleanMachine.Generic;
using CleanMachine.Interfaces;
using System;

namespace CleanMachine
{
    public class MachineBuilder<TState> : IDisposable where TState : struct
    {
        public MachineBuilder(StateMachine<TState> machine)
        {
            Machine = machine;
        }

        public StateMachine<TState> Machine { get; private set; }

        /// <summary>
        /// Get a <see cref="MachineBuilder{TState}"/> instance for the supplied <see cref="StateMachine{TState}"/>.
        /// The StateMachine will be editable until the <see cref="MachineBuilder{TState}"/> is disposed.
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static MachineBuilder<TState> Edit(StateMachine<TState> machine)
        {
            var instance = new MachineBuilder<TState>(machine);
            machine.Edit();
            return instance;
        }

        public void Dispose()
        {
            Machine.CompleteEdit();
        }

        public StateBuilder<TState> EditState(TState state)
        {
            if (!Machine.Editable)
            {
                throw new InvalidOperationException($"StateMachine {Machine.Name} must be in editable in order to modify a state.");
            }

            return new StateBuilder<TState>(Machine, state);
        }

        public void AddDoBehavior(TState state, Action<IState> behavior)
        {
            if (!Machine.Editable)
            {
                throw new InvalidOperationException($"StateMachine {Machine.Name} must be in editable in order to modify a state.");
            }

            (Machine[state] as State).AddDoBehavior(behavior);
        }
    }
}
