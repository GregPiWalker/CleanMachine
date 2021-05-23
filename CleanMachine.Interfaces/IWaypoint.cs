using System;

namespace CleanMachine.Interfaces
{
    public interface IWaypoint
    {
        object Juncture { get; }
    }

    public struct Waypoint : IWaypoint
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="juncture">An object that is a point on the signal flow path.</param>
        public Waypoint(object juncture)
        {
            Juncture = juncture;
        }

        public object Juncture { get; set; }
    }

    public struct DataWaypoint : IWaypoint
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="juncture">An object that is a point on the data flow path.</param>
        /// <param name="signal">A signal supplied by the juncture object.</param>
        public DataWaypoint(object juncture, object signal)
        {
            Juncture = juncture;
            Signal = signal;
        }

        public object Juncture { get; set; }

        public object Signal { get; set; }
    }
}
