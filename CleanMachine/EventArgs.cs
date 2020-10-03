using System;
using CleanMachine.Generic;

namespace CleanMachine
{
    public class SignalEventArgs : EventArgs
    {
        public object Cause { get; set; }

        public string Signal { get; set; }
    }

    public class TriggerEventArgs : SignalEventArgs
    {
        internal IDisposable TriggerContext { get; set; }

        public TriggerBase Trigger { get; set; }

        public EventArgs CauseArgs { get; set; }
    }

    public class StateChangedEventArgs<TState> : EventArgs
    {
        public TState PreviousState { get; set; }

        public TState ResultingState { get; set; }

        public Interfaces.TransitionEventArgs TransitionArgs { get; set; }
    }

    public class StateEnteredEventArgs<TState> : EventArgs where TState : struct
    {
        public TState State { get; set; }

        public Interfaces.TransitionEventArgs TransitionArgs { get; set; }

        public TState EnteredFromState
        {
            get
            {
                if (TransitionArgs == null || TransitionArgs.Transition == null)
                {
                    return (TState)Enum.Parse(typeof(TState), StateMachine<TState>.RequiredCommonStateValue);
                }

                return TransitionArgs.Transition.Supplier.Name.ToEnum<TState>();
            }
        }
    }

    public class StateExitedEventArgs<TState> : EventArgs where TState : struct
    {
        public TState State { get; set; }

        public Interfaces.TransitionEventArgs TransitionArgs { get; set; }

        public TState ExitedToState
        {
            get
            {
                if (TransitionArgs == null || TransitionArgs.Transition == null)
                {
                    return (TState)Enum.Parse(typeof(TState), StateMachine<TState>.RequiredCommonStateValue);
                }

                return TransitionArgs.Transition.Consumer.Name.ToEnum<TState>();
            }
        }
    }

    public class TransitionEventArgs : EventArgs
    {
        public SignalEventArgs SignalArgs { get; set; }

        public Transition Transition { get; set; }
    }

    public class EventArgs<TArg> : EventArgs
    {
        public TArg Argument { get; set; }
    }
}
