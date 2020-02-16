using System;
using System.Reactive.Disposables;

namespace CleanMachine
{
    public class TransitionEventArgs : EventArgs
    {
        public TriggerEventArgs TriggerArgs { get; set; }

        public Transition Transition { get; set; }
    }

    //public class StateEnteredEventArgs : EventArgs
    //{
    //    public State State { get; set; }

    //    public TransitionEventArgs TransitionArgs { get; set; }
    //}

    //public class StateExitedEventArgs : EventArgs
    //{
    //    public State State { get; set; }

    //    public TransitionEventArgs TransitionArgs { get; set; }
    //}

    public class TriggerEventArgs : EventArgs
    {
        public BooleanDisposable TriggerContext { get; set; }

        public object Cause { get; set; }

        public EventArgs CauseArgs { get; set; }

        public TriggerBase Trigger { get; set; }
    }

    public class StateChangedEventArgs<TState> : EventArgs
    {
        public TState PreviousState { get; set; }

        public TState CurrentState { get; set; }

        public Interfaces.TransitionEventArgs TransitionArgs { get; set; }
    }

    public class StateEnteredEventArgs<TState> : EventArgs
    {
        public TState State { get; set; }

        public Interfaces.TransitionEventArgs TransitionArgs { get; set; }
    }

    public class StateExitedEventArgs<TState> : EventArgs
    {
        public TState State { get; set; }

        public Interfaces.TransitionEventArgs TransitionArgs { get; set; }
    }
}
