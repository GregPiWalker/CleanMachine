using CleanMachine.Interfaces.Generic;
using log4net;
using System;

namespace CleanMachine.Generic
{
    public class Constraint<TParam> : IConstraint
    {
        private readonly ILog _logger;

        public Constraint(string name, Func<TParam, bool> condition, ILog logger)
            : this(name, condition, null, logger)
        {
            _logger = logger;
            Name = name;
            Condition = condition;
        }

        /// <summary>
        /// Create a Constraint with a generically typed condition that uses an argument transform 
        /// to convert the internally supplied generic parameter into the TParam type desired by the condition.
        /// The type conversion will occur on every condition evaluation.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <param name="transform"></param>
        /// <param name="logger"></param>
        public Constraint(string name, Func<TParam, bool> condition, Func<object, TParam> transform, ILog logger)
        {
            _logger = logger;
            Name = name;
            Condition = condition;
            Transform = transform;
        }

        /// <summary>
        /// Create a Constraint with a generically typed condition that uses a pre-supplied argument
        /// to pass to the condition at evaluation time.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="condition"></param>
        /// <param name="preconfiguredArg"></param>
        /// <param name="logger"></param>
        public Constraint(string name, Func<TParam, bool> condition, TParam preconfiguredArg, ILog logger)
            : this(name, condition, logger)
        {
            EvaluationArgument = preconfiguredArg;
        }


        /// <summary>
        /// A function that defines the condition to be evaluated for this Constraint.
        /// </summary>
        public Func<TParam, bool> Condition { get; protected set; }

        /// <summary>
        /// A function that transforms the generic type that is internally provided as the
        /// evaluation argument into a generic type that the condition needs.
        /// </summary>
        public Func<object, TParam> Transform { get; protected set; }

        public bool VerboseLogging { get; set; }

        public string Name { get; private set; }

        public bool LastResult { get; protected set; }

        public object EvaluationArgument { get; internal set; }

        public override string ToString()
        {
            return "[" + Name + "]";
        }

        public bool IsTrue()
        {
            return IsTrue(EvaluationArgument);
        }

        public bool IsTrue(object argument)
        {
            try
            {
                LastResult = Evaluate(EvaluationArgument ?? argument);
                return LastResult;
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.GetType().Name} while evaluating '{Name}' constraint.", ex);
                throw;
            }
        }

        /// <summary>
        /// Evaluate the condition for this Constraint.
        /// </summary>
        /// <returns>bool</returns>
        protected virtual bool Evaluate(object argument)
        {
            if (Condition == null)
            {
                //_logger.Debug($"Failed to evaluate condition with '{Name}' constraint.");
                return false;
            }

            if (Transform == null && !(argument is TParam))
            {
                _logger.Debug($"Failed to evaluate condition with '{Name}' constraint. The generic type parameter did not match the expected type.");
                return false;
            }

            // Favor the transform if there is one.
            TParam convertedArg = Transform == null ? (TParam)argument : Transform(argument);

            var result = Condition(convertedArg);
            if (VerboseLogging && result && !string.IsNullOrEmpty(Name))
            {
                _logger.Debug($"Condition was satisfied for '{Name}' constraint.");
            }
            return result;
        }
    }
}
