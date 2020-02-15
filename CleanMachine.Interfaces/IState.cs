using System;
using System.Collections.ObjectModel;

namespace CleanMachine.Interfaces
{
    public interface IState
    {
        event EventHandler<StateEnteredEventArgs> EntryInitiated;
        event EventHandler<StateEnteredEventArgs> EntryCompleted;
        event EventHandler<StateExitedEventArgs> ExitInitiated;
        event EventHandler<StateExitedEventArgs> ExitCompleted;
        event EventHandler<TransitionEventArgs> TransitionSucceeded;
        event EventHandler<TransitionEventArgs> TransitionFailed;

        string Name { get; }

        bool IsCurrentState { get; }

        ReadOnlyCollection<ITransition> Transitions { get; }
    }
}
