using CleanMachine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanMachine
{
    public class Binder
    {
        public Binder()
        {
        }

        public string[] ReflexKeys { get; set; }

        //public Transition Transition { get; set; }

        public State ToState { get; set; }

        public State FromState { get; set; }

        public Guid ToId { get; set; }

        public Guid FromId { get; set; }

        public IConstraint Guard { get; set; }
    }
}
