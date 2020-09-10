using System;

namespace CleanMachine.Generic
{
    public class MachineEditor<TState> : IDisposable where TState : struct
    {
        public MachineEditor(StateMachine<TState> machine)
        {
            Machine = machine;
        }

        public StateMachine<TState> Machine { get; private set; }

        /// <summary>
        /// Get a <see cref="MachineEditor{TState}"/> instance for the supplied <see cref="StateMachine{TState}"/>.
        /// The StateMachine will be editable until the <see cref="MachineEditor{TState}"/> is disposed.
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static MachineEditor<TState> Edit(StateMachine<TState> machine)
        {
            var instance = new MachineEditor<TState>(machine);
            machine.Edit();
            return instance;
        }

        public void Dispose()
        {
            Machine.CompleteEdit();
        }

        public StateEditor<TState> EditState(TState state)
        {
            if (!Machine.Editable)
            {
                throw new InvalidOperationException($"StateMachine {Machine.Name} must be in editable in order to modify a state.");
            }

            return new StateEditor<TState>(Machine, state);
        }
    }
}
