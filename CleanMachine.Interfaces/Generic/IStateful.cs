using System;

namespace CleanMachine.Interfaces.Generic
{
    public interface IStateful<TState> where TState : struct
    {
        IStateMachine<TState> StateMachine { get; }
    }
}
