using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanMachine
{
    public class Binder
    {
        public Binder(Transition transition)
        {
            Transition = transition;
        }

        public string[] ReflexKeys { get; set; }

        public Transition Transition { get; set; }

        public State ToState { get; set; }

        public State FromState { get; set; }

        public string ToId { get; set; }

        public string FromId { get; set; }

        public bool BindTo()
        {
            return false;
        }

        public bool BindFrom()
        {
            return false;
        }
    }
}
