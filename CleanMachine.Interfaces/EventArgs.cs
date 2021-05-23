using NodaTime;
using System;
using System.Collections.Generic;

namespace CleanMachine.Interfaces
{
    public class TripEventArgs : EventArgs
    {
        public TripEventArgs(IDisposable visitorId)
        {
            VisitorIdentifier = visitorId;
        }

        /// <summary>
        /// Gets the trip route as a linked list of waypoints.
        /// </summary>
        public LinkedList<IWaypoint> Waypoints { get; private set; } = new LinkedList<IWaypoint>();

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
    }

    public class TransitionEventArgs : EventArgs
    {
        public object Signal { get; set; }

        public object SignalData { get; set; }

        public ITrigger Trigger { get; set; }

        public LinkedList<IWaypoint> TripRoute { get; set; }

        public ITransition Transition { get; set; }
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
