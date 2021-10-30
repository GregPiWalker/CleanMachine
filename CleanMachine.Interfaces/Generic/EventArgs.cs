using System;

namespace CleanMachine.Interfaces.Generic
{
    public class StateChangedEventArgs<TState> : EventArgs
    {
        public TState PreviousState { get; set; }

        public TState ResultingState { get; set; }

        public TransitionEventArgs TransitionArgs { get; set; }
    }
}
