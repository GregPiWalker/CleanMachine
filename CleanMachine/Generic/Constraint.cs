using CleanMachine.Interfaces.Generic;
using System;

namespace CleanMachine.Generic
{
    public class Constraint<TParam> : IConstraint<TParam>
    {
        public Constraint(string name, Func<TParam, bool> condition)
        {
            Name = name;
            Condition = condition;
        }

        public Constraint(string name, Func<TParam, bool> condition, TParam preconfiguredArg)
            : this(name, condition)
        {
            PreconfiguredArgument = preconfiguredArg;
        }


        /// <summary>
        /// A function that defines the condition to be evaluated for this Constraint.
        /// </summary>
        public Func<TParam, bool> Condition { get; protected set; }

        public bool SuppressLogging { get; set; }

        public string Name { get; private set; }

        public bool LastResult { get; protected set; }

        protected TParam PreconfiguredArgument { get; set; }

        public override string ToString()
        {
            return "[" + Name + "]";
        }

        public bool IsTrue()
        {
            return IsTrue(PreconfiguredArgument);
        }

        public bool IsTrue(TParam argument)
        {
            try
            {
                LastResult = Evaluate(argument);
                return LastResult;
            }
            catch (Exception ex)
            {
                //LogService.Log(LogType, LogMessageType.Error, GetType().Name,
                //    string.Format(CultureInfo.InvariantCulture, "Exception while evaluating '{0}' constraint.", Name), ex);
                throw;
            }
        }

        /// <summary>
        /// Evaluate the condition for this Constraint.
        /// </summary>
        /// <returns>bool</returns>
        protected virtual bool Evaluate(TParam argument)
        {
            if (Condition == null)
            {
                return false;
            }

            var result = Condition(argument);
            if (!SuppressLogging && result && !string.IsNullOrEmpty(Name))
            {
                //LogService.Log(LogType, LogMessageType.Trace, GetType().Name,
                //    string.Format(CultureInfo.InvariantCulture, "Condition was satisfied for '{0}' constraint.", Name));
            }
            return result;
        }
    }
}
