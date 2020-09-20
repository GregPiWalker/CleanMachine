using System;

namespace CleanMachine.Interfaces.Generic
{
    public interface IConstraint : Interfaces.IConstraint
    {
        bool IsTrue(object argument);

        object EvaluationArgument { get; }
    }
}
