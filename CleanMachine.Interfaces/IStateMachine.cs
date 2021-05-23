using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CleanMachine.Interfaces
{
    public interface IStateMachine
    {
        /// <summary>
        /// Gets the name of this <see cref="IStateMachine"/>
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the current state.
        /// </summary>
        IState CurrentState { get; }

        /// <summary>
        /// 
        /// </summary>
        ReadOnlyCollection<IState> States { get; }

        /// <summary>
        /// Gets a value indicating whether the machine should automatically attempt another
        /// state transition after a successful transition.
        /// </summary>
        bool AutoAdvance { get; }

        /// <summary>
        /// Gets the list of <see cref="IWaypoint"/>s that occurred in the most recent successful trip.
        /// </summary>
        LinkedList<IWaypoint> History { get; }
    }
}
