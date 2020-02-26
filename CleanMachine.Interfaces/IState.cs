using System;
using System.Collections.ObjectModel;

namespace CleanMachine.Interfaces
{
    public interface IState
    {
        event EventHandler<StateEnteredEventArgs> Entered;
        event EventHandler<StateExitedEventArgs> Exited;
        event EventHandler<TransitionEventArgs> TransitionSucceeded;
        event EventHandler<TransitionEventArgs> TransitionFailed;

        string Name { get; }

        bool IsCurrentState { get; }

        ReadOnlyCollection<ITransition> Transitions { get; }

        void LogDiagnostics();
    }
}
