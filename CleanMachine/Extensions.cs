using System;

namespace CleanMachine
{
    public static class Extensions
    {
        internal static Interfaces.TriggerEventArgs ToITriggerArgs(this TriggerEventArgs internalArgs)
        {
            var triggerArgs = new Interfaces.TriggerEventArgs()
            {
                Trigger = internalArgs.Trigger,
                Cause = internalArgs.Cause,
                CauseArgs = internalArgs.CauseArgs
            };

            return triggerArgs;
        }

        internal static Interfaces.TransitionEventArgs ToITransitionArgs(this TriggerEventArgs internalArgs, Transition transition)
        {
            var transitionArgs = new Interfaces.TransitionEventArgs()
            {
                TriggerArgs = internalArgs.ToITriggerArgs(),
                Transition = transition
            };

            return transitionArgs;
        }

        internal static Interfaces.TransitionEventArgs ToITransitionArgs(this Transition transition, TriggerEventArgs internalArgs)
        {
            var transitionArgs = new Interfaces.TransitionEventArgs()
            {
                TriggerArgs = internalArgs == null ? new Interfaces.TriggerEventArgs() : internalArgs.ToITriggerArgs(),
                Transition = transition
            };

            return transitionArgs;
        }

        internal static Interfaces.StateEnteredEventArgs ToIStateEnteredArgs(this Transition transition, TriggerEventArgs internalArgs)
        {
            var stateArgs = new Interfaces.StateEnteredEventArgs()
            {
                State = transition.To,
                TransitionArgs = transition.ToITransitionArgs(internalArgs)
            };

            return stateArgs;
        }

        internal static Interfaces.StateExitedEventArgs ToIStateExitedArgs(this Transition transition, TriggerEventArgs internalArgs)
        {
            var stateArgs = new Interfaces.StateExitedEventArgs()
            {
                State = transition.From,
                TransitionArgs = transition.ToITransitionArgs(internalArgs)
            };

            return stateArgs;
        }

        internal static StateChangedEventArgs<TState> ToIStateChangedArgs<TState>(this Transition transition, TriggerEventArgs internalArgs)
        {
            var stateArgs = new StateChangedEventArgs<TState>()
            {
                CurrentState = transition.To.ToEnum<TState>(),
                PreviousState = transition.From.ToEnum<TState>(),
                TransitionArgs = transition.ToITransitionArgs(internalArgs)
            };

            return stateArgs;
        }

        internal static StateEnteredEventArgs<TState> ToStateEnteredArgs<TState>(this Interfaces.StateEnteredEventArgs args)
        {
            var stateArgs = new StateEnteredEventArgs<TState>()
            {
                State = args.State.ToEnum<TState>(),
                TransitionArgs = args.TransitionArgs == null ? new Interfaces.TransitionEventArgs() : args.TransitionArgs
            };

            return stateArgs;
        }

        internal static StateExitedEventArgs<TState> ToStateExitedArgs<TState>(this Interfaces.StateExitedEventArgs args)
        {
            var stateArgs = new StateExitedEventArgs<TState>()
            {
                State = args.State.ToEnum<TState>(),
                TransitionArgs = args.TransitionArgs == null ? new Interfaces.TransitionEventArgs() : args.TransitionArgs
            };

            return stateArgs;
        }

        internal static TransitionEventArgs ToTransitionArgs(this Transition transition, TriggerEventArgs internalArgs)
        {
            var transitionArgs = new TransitionEventArgs()
            {
                TriggerArgs = internalArgs ?? new TriggerEventArgs(),
                Transition = transition
            };

            return transitionArgs;
        }

        public static TState ToEnum<TState>(this Interfaces.IState state)
        {
            return state.Name.ToEnum<TState>();
        }

        public static TState ToEnum<TState>(this string state)
        {
            return (TState)Enum.Parse(typeof(TState), state);
        }
    }
}
