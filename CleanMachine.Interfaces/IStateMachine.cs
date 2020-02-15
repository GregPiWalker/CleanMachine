using System;
using System.Collections.ObjectModel;

namespace CleanMachine.Interfaces
{
    public interface IStateMachine
    {
        ReadOnlyCollection<IState> States { get; }
    }
}
