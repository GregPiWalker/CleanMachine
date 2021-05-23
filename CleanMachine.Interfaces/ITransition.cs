using System;

namespace CleanMachine.Interfaces
{
    public interface ITransition
    {
        /// <summary>
        /// Raised when an attempt to traverse this ITransition has succeeded.
        /// </summary>
        event EventHandler<TransitionEventArgs> Succeeded;

        /// <summary>
        /// Raised when an attempt to traverse this ITransition has failed.
        /// </summary>
        event EventHandler<TransitionEventArgs> Failed;

        string Name { get; }

        /// <summary>
        /// Gets meta-information about this ITransition
        /// </summary>
        string Stereotype { get; }

        /// <summary>
        /// Gets the IState node that consumes this ITransition,
        /// meaning it resides at the end-point.
        /// </summary>
        IState Consumer { get; }

        /// <summary>
        /// Gets the IState node that supplies this ITransition,
        /// meaning it resides at the begin-point.
        /// </summary>
        IState Supplier { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ITransition"/> has any <see cref="ITrigger"/>s
        /// or not.  It is passive if there are no triggers.
        /// </summary>
        bool IsPassive { get; }

        void LogDiagnostics();
    }
}
