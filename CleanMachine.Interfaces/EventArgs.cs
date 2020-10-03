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

        public string EnteredFromState
        {
            get
            {
                if (TransitionArgs == null || TransitionArgs.Transition == null)
                {
                    return string.Empty;
                }

                return TransitionArgs.Transition.Supplier.Name;
            }
        }
    }

    public class StateExitedEventArgs : EventArgs
    {
        public IState State { get; set; }

        public TransitionEventArgs TransitionArgs { get; set; }

        public string ExitedToState
        {
            get
            {
                if (TransitionArgs == null || TransitionArgs.Transition == null)
                {
                    return string.Empty;
                }

                return TransitionArgs.Transition.Consumer.Name;
            }
        }
    }

    public class SignalEventArgs : EventArgs
    {
        public object Cause { get; set; }

        public string Signal { get; set; }
    }

    public class TriggerEventArgs : SignalEventArgs
    {
        public ITrigger Trigger { get; set; }

        public EventArgs CauseArgs { get; set; }
    }
}
