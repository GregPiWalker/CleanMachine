using System;

namespace CleanMachine.Interfaces
{
    public class TransitionEventArgs : EventArgs
    {
        public SignalEventArgs TriggerArgs { get; set; }

        public ITransition Transition { get; set; }
    }

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

    public class SignalEventArgs : EventArgs
    {
        public object Cause { get; set; }

        public EventArgs CauseArgs { get; set; }
    }

    public class TriggerEventArgs : SignalEventArgs
    {
        public ITrigger Trigger { get; set; }
    }
}
