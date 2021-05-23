using System;

namespace CleanMachine.Interfaces
{
    public interface ITransitionBehavior
    {
        IBehavior Effect { get; set; }
    }
}
