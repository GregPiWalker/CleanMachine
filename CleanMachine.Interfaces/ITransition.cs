using System;

namespace CleanMachine.Interfaces
{
    public interface ITransition
    {
        /// <summary>
        /// Raised to indicate that this transition was successfully traversed.
        /// </summary>
        event EventHandler<TransitionEventArgs> Succeeded;

        /// <summary>
        /// Raised to indicate that an attempt to traverse this transition has failed.
        /// </summary>
        event EventHandler<TransitionEventArgs> Failed;

        string Name { get; }

        IState Consumer { get; }

        IState Supplier { get; }        

        void LogDiagnostics();
    }
}
