using System;
using Unity;

namespace CleanMachine.Interfaces
{
    public interface IBehavior
    {
        event EventHandler<ClockedEventArgs> Finished;
        event EventHandler<FaultedEventArgs> Faulted;

        string Description { get; }

        string Name { get; }

        /// <summary>
        /// TODO: perhaps change the IUnityContainer out for something more mutable.
        /// </summary>
        /// <param name="runtimeContainer"></param>
        void Invoke(IUnityContainer runtimeContainer);
    }
}
