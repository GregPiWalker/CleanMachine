﻿using System;

namespace CleanMachine.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITrigger
    {
        /// <summary>
        /// 
        /// </summary>
        //event EventHandler<TriggerEventArgs> Triggered;

        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        //IConstraint Filter { get; }

        /// <summary>
        /// 
        /// </summary>
        //object Source { get; }

        //Type SourceType { get; }

        /// <summary>
        /// Indicates whether this trigger is connected and responding to surrounding events.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 
        /// </summary>
        bool IsSourceLazy { get; }

        /// <summary>
        /// Activate this trigger, making it responsive to surrounding events.
        /// </summary>
        /// <param name="validity"></param>
        void Activate(IDisposable validity);

        /// <summary>
        /// Disconnects the trigger.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Conditionally trip the trigger.  If tripped, the <see cref="Triggered"/> event is raised.
        /// If the Guard is not satisfied, then the trigger is not tripped.
        /// </summary>
        /// <param name="metadata">The optional trigger event metadata.</param>
        //void Trip(ITriggerData metadata = null);
    }
}
