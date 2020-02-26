using System;

namespace CleanMachine.Interfaces
{
    public interface ITransition
    {
        event EventHandler<TransitionEventArgs> Succeeded;
        event EventHandler<TransitionEventArgs> Failed;

        string Name { get; }

        IState Consumer { get; }

        IState Supplier { get; }        

        void LogDiagnostics();
    }
}
