using System;

namespace CleanMachine.Interfaces.Generic
{
    public interface IConstraint<TParam> : IConstraint
    {
        bool IsTrue(TParam argument);
    }
}
