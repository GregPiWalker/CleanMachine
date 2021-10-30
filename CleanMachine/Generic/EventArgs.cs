using System;

namespace CleanMachine.Generic
{
    //public class StateChangedEventArgs<TState> : EventArgs
    //{
    //    public TState PreviousState { get; set; }

    //    public TState ResultingState { get; set; }

    //    public Interfaces.TransitionEventArgs TransitionArgs { get; set; }
    //}

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
}
