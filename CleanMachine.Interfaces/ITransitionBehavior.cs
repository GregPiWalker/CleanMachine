using System;

namespace CleanMachine.Interfaces
{
    public interface ITransitionBehavior
    {
        Action Effect { get; set; }
    }
}
