using System;

namespace CleanMachine.Interfaces
{
    public class TransitionEventArgs : EventArgs
    {
        public TriggerEventArgs TriggerArgs { get; set; }

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

    public class TriggerEventArgs : EventArgs
    {
        public object Cause { get; set; }

        public EventArgs CauseArgs { get; set; }

        public ITrigger Trigger { get; set; }
    }
}
