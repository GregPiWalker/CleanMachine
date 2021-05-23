using System;
using Unity;

namespace CleanMachine.Interfaces
{
    public interface IStateBehavior
    {
        event EventHandler<StateEnteredEventArgs> EntryInitiated;
        event EventHandler<StateExitedEventArgs> ExitInitiated;

        void SetEntryBehavior(IBehavior behavior);
        void SetEntryBehavior(Action<IUnityContainer> action);

        void SetExitBehavior(IBehavior behavior);
        void SetExitBehavior(Action<IUnityContainer> action);

        void AddDoBehavior(IBehavior behavior);
        void AddDoBehavior(Action<IUnityContainer> action);
    }
}
