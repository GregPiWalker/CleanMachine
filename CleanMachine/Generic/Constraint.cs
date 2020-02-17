﻿using CleanMachine.Interfaces.Generic;
using log4net;
using System;

namespace CleanMachine.Generic
{
    public class Constraint<TParam> : IConstraint<TParam>
    {
        private readonly ILog _logger;

        public Constraint(string name, Func<TParam, bool> condition, ILog logger)
        {
            _logger = logger;
            Name = name;
            Condition = condition;
        }

        public Constraint(string name, Func<TParam, bool> condition, TParam preconfiguredArg, ILog logger)
            : this(name, condition, logger)
        {
            PreconfiguredArgument = preconfiguredArg;
        }


        /// <summary>
        /// A function that defines the condition to be evaluated for this Constraint.
        /// </summary>
        public Func<TParam, bool> Condition { get; protected set; }
        
        public bool VerboseLogging { get; set; }

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
                _logger.Error($"{ex.GetType().Name} while evaluating '{Name}' constraint.", ex);
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
            if (VerboseLogging && result && !string.IsNullOrEmpty(Name))
            {
                _logger.Debug($"Condition was satisfied for '{Name}' constraint.");
            }
            return result;
        }
    }
}