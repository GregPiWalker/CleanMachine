using System;

namespace CleanMachine.Interfaces
{
    public interface IStateful
    {
        IStateMachine StateMachine { get; }
    }
}
