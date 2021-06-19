using CleanMachine.Generic;
using CleanMachine.Interfaces;
using System;
using System.Reflection;
using Unity;

namespace CleanMachine
{
    public static class Extensions
    {
        internal static TransitionEventArgs ToTransitionArgs(this TripEventArgs internalArgs, Transition transition)
        {
            var origin = (DataWaypoint)internalArgs.GetTripOrigin();
            var transitionArgs = new TransitionEventArgs()
            {
                Signal = origin.Juncture,
                SignalData = origin.Signal,
                Trigger = internalArgs?.FindTrigger(),
                TripRoute = internalArgs == null ? null : internalArgs.Waypoints,
                Transition = transition
            };

            return transitionArgs;
        }

        internal static TransitionEventArgs ToTransitionArgs(this Transition transition, TripEventArgs internalArgs)
        {
            var origin = (DataWaypoint)internalArgs.GetTripOrigin();
            var transitionArgs = new TransitionEventArgs()
            {
                Signal = origin.Juncture,
                SignalData = origin.Signal,
                Trigger = internalArgs?.FindTrigger(),
                TripRoute = internalArgs == null ? null : internalArgs.Waypoints,
                Transition = transition
            };

            return transitionArgs;
        }

        internal static StateEnteredEventArgs ToIStateEnteredArgs(this Transition transition, TripEventArgs internalArgs)
        {
            var stateArgs = new StateEnteredEventArgs()
            {
                State = transition.To,
                TransitionArgs = transition.ToTransitionArgs(internalArgs)
            };

            return stateArgs;
        }

        internal static StateExitedEventArgs ToIStateExitedArgs(this Transition transition, TripEventArgs internalArgs)
        {
            var stateArgs = new StateExitedEventArgs()
            {
                State = transition.From,
                TransitionArgs = transition.ToTransitionArgs(internalArgs)
            };

            return stateArgs;
        }

        internal static StateChangedEventArgs<TState> ToIStateChangedArgs<TState>(this Transition transition, TripEventArgs internalArgs)
        {
            var stateArgs = new StateChangedEventArgs<TState>()
            {
                ResultingState = transition.To.ToEnum<TState>(),
                PreviousState = transition.From.ToEnum<TState>(),
                TransitionArgs = transition.ToTransitionArgs(internalArgs)
            };

            return stateArgs;
        }

        internal static StateEnteredEventArgs<TState> ToStateEnteredArgs<TState>(this StateEnteredEventArgs args) where TState : struct
        {
            var stateArgs = new StateEnteredEventArgs<TState>()
            {
                State = args.State.ToEnum<TState>(),
                TransitionArgs = args.TransitionArgs == null ? new TransitionEventArgs() : args.TransitionArgs
            };

            return stateArgs;
        }

        internal static StateExitedEventArgs<TState> ToStateExitedArgs<TState>(this StateExitedEventArgs args) where TState : struct
        {
            var stateArgs = new StateExitedEventArgs<TState>()
            {
                State = args.State.ToEnum<TState>(),
                TransitionArgs = args.TransitionArgs == null ? new TransitionEventArgs() : args.TransitionArgs
            };

            return stateArgs;
        }

        public static TState ToEnum<TState>(this IState state)
        {
            return state.Name.ToEnum<TState>();
        }

        public static TState ToEnum<TState>(this string state)
        {
            return (TState)Enum.Parse(typeof(TState), state);
        }

        public static bool IsEnum<TEnum>(this string convert) where TEnum : struct
        {
            TEnum e;
            return Enum.TryParse(convert, out e);
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

        public static bool HasStereotype<TEnum>(this ITransition transition, TEnum stereotype) where TEnum : struct
        {
            if (transition.Stereotype.Equals(stereotype.ToString()))
            {
                return true;
            }

            return false;
        }

        public static bool HasAnyStereotype<TEnum>(this ITransition transition, TEnum stereotype, params TEnum[] otherStereotypes) where TEnum : struct
        {
            if (transition.Stereotype.Equals(stereotype.ToString()))
            {
                return true;
            }

            foreach (var st in otherStereotypes)
            {
                if (transition.Stereotype.Equals(st.ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasTypeRegistration<TReg>(this IUnityContainer container)
        {
            try
            {
                container.Resolve<TReg>();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool HasTypeRegistration<TReg>(this IUnityContainer container, string key)
        {
            try
            {
                container.Resolve<TReg>(key);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static TReg TryGetInstance<TReg>(this IUnityContainer container, string instanceName) where TReg : class
        {
            try
            {
                return container.Resolve<TReg>(instanceName);
            }
            catch
            {
                return null;
            }
        }

        public static TReg TryGetTypeRegistration<TReg>(this IUnityContainer container) where TReg : class
        {
            try
            {
                return container.Resolve<TReg>();
            }
            catch
            {
                return null;
            }
        }

        public static TReg? TryGetValue<TReg>(this IUnityContainer container, string instanceName) where TReg : struct
        {
            try
            {
                return container.Resolve<TReg>(instanceName);
            }
            catch
            {
                return null;
            }
        }

        public static TReg? TryGetValueRegistration<TReg>(this IUnityContainer container) where TReg : struct
        {
            try
            {
                return container.Resolve<TReg>();
            }
            catch
            {
                return null;
            }
        }

        //internal static Interfaces.SignalEventArgs ToISignalArgs(this SignalEventArgs internalArgs)
        //{
        //    Interfaces.SignalEventArgs signalArgs;
        //    var triggerArgs = internalArgs as TriggerEventArgs;
        //    if (triggerArgs != null)
        //    {
        //        signalArgs = new Interfaces.TriggerEventArgs()
        //        {
        //            Trigger = triggerArgs.Trigger,
        //            Cause = triggerArgs.Cause,
        //            CauseArgs = triggerArgs.CauseArgs,
        //            Signal = internalArgs.Signal
        //        };
        //    }
        //    else
        //    {
        //        signalArgs = new Interfaces.SignalEventArgs()
        //        {
        //            Cause = internalArgs.Cause,
        //            Signal = internalArgs.Signal
        //        };
        //    }

        //    return signalArgs;
        //}

        //internal static Interfaces.TriggerEventArgs ToITriggerArgs(this TriggerEventArgs internalArgs)
        //{
        //    var triggerArgs = new Interfaces.TriggerEventArgs()
        //    {
        //        Trigger = internalArgs.Trigger,
        //        Cause = internalArgs.Cause,
        //        CauseArgs = internalArgs.CauseArgs
        //    };

        //    return triggerArgs;
        //}

        //internal static Interfaces.TransitionEventArgs ToITransitionArgs(this TriggerEventArgs internalArgs, Transition transition)
        //{
        //    var transitionArgs = new Interfaces.TransitionEventArgs()
        //    {
        //        TriggerArgs = internalArgs == null ? null : internalArgs.ToITriggerArgs(),
        //        Transition = transition
        //    };

        //    return transitionArgs;
        //}
    }
}
