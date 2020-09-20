using System;
using System.Reflection;

namespace CleanMachine
{
    public static class Extensions
    {
        internal static Interfaces.SignalEventArgs ToISignalArgs(this SignalEventArgs internalArgs)
        {
            Interfaces.SignalEventArgs signalArgs;
            var triggerArgs = internalArgs as TriggerEventArgs;
            if (triggerArgs != null)
            {
                signalArgs = new Interfaces.TriggerEventArgs()
                {
                    Trigger = triggerArgs.Trigger,
                    Cause = triggerArgs.Cause,
                    CauseArgs = triggerArgs.CauseArgs
                };
            }
            else
            {
                signalArgs = new Interfaces.SignalEventArgs()
                {
                    Cause = internalArgs.Cause,
                    CauseArgs = internalArgs.CauseArgs
                };
            }

            return signalArgs;
        }

        internal static Interfaces.TransitionEventArgs ToITransitionArgs(this SignalEventArgs internalArgs, Transition transition)
        {
            var transitionArgs = new Interfaces.TransitionEventArgs()
            {
                TriggerArgs = internalArgs == null ? null : internalArgs.ToISignalArgs(),
                Transition = transition
            };

            return transitionArgs;
        }

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

        //internal static Interfaces.TransitionEventArgs ToITransitionArgs(this TriggerEventArgs internalArgs, Transition transition)
        //{
        //    var transitionArgs = new Interfaces.TransitionEventArgs()
        //    {
        //        TriggerArgs = internalArgs == null ? null : internalArgs.ToITriggerArgs(),
        //        Transition = transition
        //    };

        //    return transitionArgs;
        //}

        internal static Interfaces.TransitionEventArgs ToITransitionArgs(this Transition transition, SignalEventArgs internalArgs)
        {
            var transitionArgs = new Interfaces.TransitionEventArgs()
            {
                TriggerArgs = internalArgs == null ? new Interfaces.SignalEventArgs() : internalArgs.ToISignalArgs(),
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

        internal static StateChangedEventArgs<TState> ToIStateChangedArgs<TState>(this Transition transition, SignalEventArgs internalArgs)
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

        internal static TransitionEventArgs ToTransitionArgs(this Transition transition, SignalEventArgs internalArgs)
        {
            var transitionArgs = new TransitionEventArgs()
            {
                SignalArgs = internalArgs ?? new SignalEventArgs(),
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

        /// <summary>
        /// Check whether this state is equal to or is a substate to the given other state.
        /// </summary>
        /// <typeparam name="TState">Type of the state enum.</typeparam>
        /// <param name="state">The query subject.</param>
        /// <param name="otherState">the other state against which to check this state.</param>
        /// <returns>True if this state is the same state or is a substate of the given other state.</returns>
        public static bool Is<TState>(this TState state, TState otherState) where TState : struct
        {
            if (state.Equals(otherState))
            {
                return true;
            }

            // Check if state is a substate of otherState.
            var substateOf = state.GetType().GetCustomAttribute(typeof(SubstateOfAttribute)) as SubstateOfAttribute;
            if (substateOf != null && otherState.ToString().Equals(substateOf.SuperstateName))
            {
                return true;
            }

            //TODO:  RECURSIVELY SCAN SUPER STATES

            return false;
        }

        /// <summary>
        /// Check whether this state is equal to or is a substate to one of the given other states.
        /// </summary>
        /// <typeparam name="TState">Type of the state enum.</typeparam>
        /// <param name="state">The query subject.</param>
        /// <param name="otherState">a mandatory other state against which to check this state.</param>
        /// <param name="otherStates">optional other states against which to check this state.</param>
        /// <returns>True if this state is the same state or is a substate of one of the given other states.</returns>
        public static bool IsOneOf<TState>(this TState state, TState otherState, params TState[] otherStates) where TState : struct
        {
            if (state.Is(otherState))
            {
                return true;
            }

            foreach (var another in otherStates)
            {
                if (state.Is(another))
                {
                    return true;
                }
            }

            // Check if state is a substate of otherStates.
            var substateOf = state.GetType().GetCustomAttribute(typeof(SubstateOfAttribute)) as SubstateOfAttribute;
            if (substateOf != null)
            {
                foreach (var other in otherStates)
                {
                    if (other.ToString().Equals(substateOf.SuperstateName))
                    {
                        return true;
                    }

                    //TODO:  RECURSIVELY SCAN SUPER STATES
                }
            }

            return false;
        }

        public static bool IsNoneOf<TState>(this TState state, TState otherState, params TState[] otherStates) where TState : struct
        {
            if (state.Is(otherState))
            {
                return false;
            }

            foreach (var another in otherStates)
            {
                if (state.Is(another))
                {
                    return false;
                }
            }

            // Check if state is a substate of otherStates.
            var substateOf = state.GetType().GetCustomAttribute(typeof(SubstateOfAttribute)) as SubstateOfAttribute;
            if (substateOf != null)
            {
                foreach (var other in otherStates)
                {
                    if (other.ToString().Equals(substateOf.SuperstateName))
                    {
                        return false;
                    }

                    //TODO:  RECURSIVELY SCAN SUPER STATES
                }
            }

            return true;
        }
    }
}
