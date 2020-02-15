using System;

namespace CleanMachine.Interfaces
{
    public class TransitionEventArgs : EventArgs
    {
        public TriggerEventArgs TriggerArgs { get; set; }

        public ITransition Transition { get; set; }
    }

    //public class StateChangedEventArgs<TState> : EventArgs
    //{
    //    public TState PreviousState { get; set; }

    //    public TState CurrentState { get; set; }

    //    public TransitionEventArgs TransitionArgs { get; set; }
    //}

    //public class StateEnteredEventArgs<TState> : EventArgs
    //{
    //    public TState State { get; set; }

    //    public TransitionEventArgs TransitionArgs { get; set; }
    //}

    //public class StateExitedEventArgs<TState> : EventArgs
    //{
    //    public TState State { get; set; }

    //    public TransitionEventArgs TransitionArgs { get; set; }

    //    //public ITransition ExitedOn { get; set; }
    //}

    public class StateEnteredEventArgs : EventArgs
    {
        public IState State { get; set; }

        public TransitionEventArgs TransitionArgs { get; set; }
    }

    public class StateExitedEventArgs : EventArgs
    {
        public IState State { get; set; }

        public TransitionEventArgs TransitionArgs { get; set; }
    }

    public class TriggerEventArgs : EventArgs
    {
        public object Cause { get; set; }

        public EventArgs CauseArgs { get; set; }

        public ITrigger Trigger { get; set; }
    }
}
