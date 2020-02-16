using CleanMachine.Interfaces;
using log4net;
using System;

namespace CleanMachine
{
    public class Constraint : IConstraint
    {
        private readonly ILog _logger;

        public Constraint(string name, Func<bool> condition, ILog logger)
        {
            _logger = logger;
            Name = name;
            Condition = condition;
        }
        
        /// <summary>
        /// A function that defines the condition to be evaluated for this Constraint.
        /// </summary>
        public Func<bool> Condition { get; protected set; }

        public bool VerboseLogging { get; set; }

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
                _logger.Error($"{ex.GetType().Name} while evaluating '{Name}' constraint.", ex);
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
            if (VerboseLogging && result && !string.IsNullOrEmpty(Name))
            {
                _logger.Debug($"Condition was satisfied for '{Name}' constraint.");
            }

            return result;
        }
    }
}
