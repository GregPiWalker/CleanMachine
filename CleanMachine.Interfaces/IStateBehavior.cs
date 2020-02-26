using System;

namespace CleanMachine.Interfaces
{
    public interface IStateBehavior
    {
        event EventHandler<StateEnteredEventArgs> EntryInitiated;
        event EventHandler<StateExitedEventArgs> ExitInitiated;

        void SetEntryBehavior(Action<ITransition> behavior);
        void SetExitBehavior(Action<ITransition> behavior);
        void AddDoBehavior(Action<IState> behavior);
    }
}
