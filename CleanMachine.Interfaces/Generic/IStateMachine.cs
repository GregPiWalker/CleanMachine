using System;
using System.Runtime.CompilerServices;

namespace CleanMachine.Interfaces.Generic
{
    public interface IStateMachine<TState> : IStateMachine where TState : struct
    {
        new event EventHandler<StateChangedEventArgs<TState>> StateChanged;

        /// <summary>
        /// Gets the current state.
        /// </summary>
        new TState CurrentState { get; }

        ///// <summary>
        ///// Gets the name of this <see cref="IStateMachine"/>
        ///// </summary>
        //string Name { get; }

        ///// <summary>
        ///// 
        ///// </summary>
        //ReadOnlyCollection<IState> States { get; }

        ///// <summary>
        ///// Gets a value indicating whether the machine should automatically attempt another
        ///// state transition after a successful transition.
        ///// </summary>
        //bool AutoAdvance { get; }

        ///// <summary>
        ///// Gets the list of <see cref="IWaypoint"/>s that occurred in the most recent successful trip.
        ///// </summary>
        //LinkedList<IWaypoint> History { get; }

        /// <summary>
        /// Try to traverse exactly one outgoing transition from the current state that leads to the supplied target state,
        /// looking for the first available transition whose guard condition succeeds.
        /// This ignores the passive quality of the attempted Transitions.
        /// </summary>
        /// <param name="toState"></param>
        /// <param name="sender"></param>
        /// <param name="callerName">Name of the calling method. (supplied by runtime).</param>
        /// <returns>True if a transition was traversed; false otherwise.</returns>
        bool TryTransitionTo(TState toState, object sender = null, [CallerMemberName] string callerName = null);
    }
}
