using CleanMachine.Interfaces.Generic;
using System;

namespace CleanMachine.Generic
{
    /// <summary>
    /// TODO: REFACTOR
    /// </summary>
    public class MachineEditor<TState> : IDisposable where TState : struct
    {
        public MachineEditor(IStateMachine<TState> machine)
        {
            Machine = machine as StateMachine<TState>;
        }

        public StateMachine<TState> Machine { get; }

        /// <summary>
        /// Get a <see cref="MachineEditor{TState}"/> instance for the supplied <see cref="StateMachine{TState}"/>.
        /// The StateMachine will be editable until the <see cref="MachineEditor{TState}"/> is disposed.
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static MachineEditor<TState> Edit(IStateMachine<TState> machine)
        {
            var instance = new MachineEditor<TState>(machine);
            instance.Machine.Edit();
            return instance;
        }

        public void Dispose()
        {
            StopEditing();
        }

        public void StopEditing()
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
