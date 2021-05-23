using System;

namespace CleanMachine.Interfaces
{
    public interface IWaypoint
    {
        object Juncture { get; }
    }

    public struct Waypoint : IWaypoint
    {
        public Waypoint(object juncture)
        {
            Juncture = juncture;
        }

        public object Juncture { get; set; }
    }

    public struct DataWaypoint : IWaypoint
    {
        public DataWaypoint(object juncture, object signal)
        {
            Juncture = juncture;
            Signal = signal;
        }

        public object Juncture { get; set; }

        public object Signal { get; set; }
    }
}
