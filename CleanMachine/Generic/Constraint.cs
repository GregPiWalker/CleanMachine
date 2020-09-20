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

        public Constraint(string name, Func<TParam, bool> condition, Func<object, TParam> transform, ILog logger)
        {
            _logger = logger;
            Name = name;
            Condition = condition;
            Transform = transform;
        }

        public Constraint(string name, Func<TParam, bool> condition, TParam preconfiguredArg, ILog logger)
            : this(name, condition, logger)
        {
            EvaluationArgument = preconfiguredArg;
        }


        /// <summary>
        /// A function that defines the condition to be evaluated for this Constraint.
        /// </summary>
        public Func<TParam, bool> Condition { get; protected set; }

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
                LastResult = Evaluate(argument);
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
            if (Condition == null || (Transform == null && !(argument is TParam)))
            {
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
