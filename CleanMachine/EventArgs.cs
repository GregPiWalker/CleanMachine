using System;

namespace CleanMachine
{
    public class SignalEventArgs : EventArgs
    {
        public object Cause { get; set; }

        public EventArgs CauseArgs { get; set; }
    }

    public class TriggerEventArgs : SignalEventArgs
    {
        internal IDisposable TriggerContext { get; set; }

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
