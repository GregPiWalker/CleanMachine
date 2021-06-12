using NodaTime;
using System;
using System.Collections.Generic;

namespace CleanMachine.Interfaces
{
    public class TripEventArgs : EventArgs
    {
        /// <summary>
        /// Create a TripEventArgs for a Trigger or Signal origin,
        /// with the given visitor identifier and origin info.
        /// </summary>
        /// <param name="visitorId"></param>
        /// <param name="tripOrigin"></param>
        public TripEventArgs(IDisposable visitorId, DataWaypoint tripOrigin)
        {
            VisitorIdentifier = visitorId;
            Waypoints.AddLast(tripOrigin);
        }

        /// <summary>
        /// Create a TripEventArgs for a Signal origin with the given origin info.
        /// </summary>
        public TripEventArgs()
        {
        }

        /// <summary>
        /// Gets the trip route as a linked list of waypoints.
        /// </summary>
        public LinkedList<IWaypoint> Waypoints { get; private set; } = new LinkedList<IWaypoint>();

        /// <summary>
        /// The VisitorIdentifier is used to compare a state visitation (Instance of Entry)
        /// against a later trigger/signal event.  It becomes useful when a state machine contains
        /// a circular path - in that case, a state could be entered, exited, and re-entered, all 
        /// while a trigger/signal instance is waiting in an event queue.  Using the VisitorIdentifier,
        /// the trigger/signal instance can be correlated to an outdated state visit.
        /// </summary>
        public IDisposable VisitorIdentifier { get; }

        public ITrigger FindTrigger()
        {
            LinkedListNode<IWaypoint> node = Waypoints.First;
            while (node != null)
            {
                if (node.Value.Juncture is ITrigger)
                {
                    return node.Value.Juncture as ITrigger;
                }

                node = node.Next;
            }

            return null;
        }

        public IState FindLastState()
        {
            LinkedListNode<IWaypoint> node = Waypoints.Last;
            while (node != null)
            {
                if (node.Value.Juncture is IState)
                {
                    return node.Value.Juncture as IState;
                }

                node = node.Previous;
            }

            return null;
        }

        public ITransition FindLastTransition()
        {
            LinkedListNode<IWaypoint> node = Waypoints.Last;
            while (node != null)
            {
                if (node.Value.Juncture is ITransition)
                {
                    return node.Value.Juncture as ITransition;
                }

                node = node.Previous;
            }

            return null;
        }

        public ITransition FindFirstTransition()
        {
            LinkedListNode<IWaypoint> node = Waypoints.First;
            while (node != null)
            {
                if (node.Value.Juncture is ITransition)
                {
                    return node.Value.Juncture as ITransition;
                }

                node = node.Next;
            }

            return null;
        }

        public DataWaypoint GetTripOrigin()
        {
            return (DataWaypoint)Waypoints.First.Value;
        }

        internal void Restart(DataWaypoint newOrigin)
        {
            Waypoints.Clear();
            Waypoints.AddLast(newOrigin);
        }
    }

    public class TransitionEventArgs : EventArgs
    {
        public object Signal { get; set; }

        public object SignalData { get; set; }

        public ITrigger Trigger { get; set; }

        public LinkedList<IWaypoint> TripRoute { get; set; }

        public ITransition Transition { get; set; }
    }

    public class StateChangedEventArgs : EventArgs
    {
        public IState PreviousState { get; set; }

        public IState ResultingState { get; set; }
    }

    public class StateEnteredEventArgs : EventArgs
    {
        public IState State { get; set; }

        public TransitionEventArgs TransitionArgs { get; set; }

        public string EnteredFromState
        {
            get
            {
                if (TransitionArgs == null || TransitionArgs.Transition == null)
                {
                    return string.Empty;
                }

                return TransitionArgs.Transition.Supplier.Name;
            }
        }
    }

    public class StateExitedEventArgs : EventArgs
    {
        public IState State { get; set; }

        public TransitionEventArgs TransitionArgs { get; set; }

        public string ExitedToState
        {
            get
            {
                if (TransitionArgs == null || TransitionArgs.Transition == null)
                {
                    return string.Empty;
                }

                return TransitionArgs.Transition.Consumer.Name;
            }
        }
    }

    public class ClockedEventArgs : EventArgs
    {
        public ClockedEventArgs(IClock clock)
        {
            CreatedTime = clock.GetCurrentInstant();
        }

        public Instant CreatedTime { get; set; }
    }

    public class FaultedEventArgs : ClockedEventArgs
    {
        public FaultedEventArgs(Exception e, IClock clock)
            : base(clock)
        {
            Fault = e;
        }

        public Exception Fault { get; set; }

        public object Sender { get; set; }
    }
}
