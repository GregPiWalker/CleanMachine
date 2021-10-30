using System;

namespace CleanMachine.Interfaces.Generic
{
    //TODO: make this generic or collapse it with the base.
    public interface IConstraint : Interfaces.IConstraint
    {
        bool IsTrue(object argument);

        object EvaluationArgument { get; }
    }
}
