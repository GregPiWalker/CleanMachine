using CleanMachine.Interfaces;
using System;

namespace CleanMachine
{
    public class Constraint : IConstraint
    {
        public Constraint(string name, Func<bool> condition)
        {
            Name = name;
            Condition = condition;
        }
        
        /// <summary>
        /// A function that defines the condition to be evaluated for this Constraint.
        /// </summary>
        public Func<bool> Condition { get; protected set; }

        public bool SuppressLogging { get; set; }

        public string Name { get; private set; }

        public bool LastResult { get; protected set; }

        public override string ToString()
        {
            return "[" + Name + "]";
        }

        public bool IsTrue()
        {
            try
            {
                LastResult = Evaluate();
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
        protected virtual bool Evaluate()
        {
            if (Condition == null)
            {
                return false;
            }

            var result = Condition();
            if (!SuppressLogging && result && !string.IsNullOrEmpty(Name))
            {
                //LogService.Log(LogType, LogMessageType.Trace, GetType().Name,
                //    string.Format(CultureInfo.InvariantCulture, "Condition was satisfied for '{0}' constraint.", Name));
            }
            return result;
        }
    }
}
