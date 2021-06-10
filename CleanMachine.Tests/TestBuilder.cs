using CleanMachine.Behavioral;
using CleanMachine.Behavioral.Behaviors;
using CleanMachine.Generic;
using CleanMachine.Interfaces;
using System;
using System.Linq;
using Unity;

namespace CleanMachine.Tests
{
    public class TestBuilder
    {
        public static void InitializeMachine<TState>(StateMachine<TState> machine, string initialState) where TState : struct
        {
            machine.Edit();
            machine.SetInitialState(initialState);

            foreach (var state in machine.States)
            {
                //state.EntryInitiated += State_EntryInitiated;
                //state.EntryCompleted += State_EntryCompleted;
                //state.ExitInitiated += State_ExitInitiated;
                //state.ExitCompleted += State_ExitCompleted;
                //state.TransitionSucceeded += State_TransitionSucceeded;
                //state.TransitionFailed += State_TransitionFailed;
            }
        }

        public static void BuildOneWayMachineWithTriggers<TState>(StateMachine<TState> machine, StateMachineTestHarness<TState> triggerSource) where TState : struct
        {
            for (int i = 0; i < machine.States.Count; i++)
            {
                if (i + 1 < machine.States.Count)
                {
                    var trigger = new Trigger<StateMachineTestHarness<TState>, EventArgs>(triggerSource, "TestTrigger", machine.TriggerScheduler, machine.Logger);
                    var transition = machine.CreateTransition(machine.States[i].Name, machine.States[i + 1].Name);
                    transition.Edit();
                    transition.AddTrigger(trigger);

                    //transition.Succeeded += Transition_Succeeded;
                    //transition.Failed += Transition_Failed;
                }
            }
        }

        public static void BuildOneWayPassiveMachine<TState>(StateMachine<TState> machine) where TState : struct
        {
            for (int i = 0; i < machine.States.Count; i++)
            {
                if (i + 1 < machine.States.Count)
                {
                    var transition = machine.CreateTransition(machine.States[i].Name, machine.States[i + 1].Name);
                    transition.Edit();

                    //transition.Succeeded += Transition_Succeeded;
                    //transition.Failed += Transition_Failed;
                }
            }
        }

        public static void BuildCircularMachineWithTriggers<TState>(StateMachine<TState> machine, StateMachineTestHarness<TState> triggerSource) where TState : struct
        {
            BuildOneWayMachineWithTriggers(machine, triggerSource);

            // Transition from the last state back to the first.
            int last = machine.States.Count - 1;
            var trigger = new Trigger<StateMachineTestHarness<TState>, EventArgs>(triggerSource, "TestTrigger", machine.TriggerScheduler, machine.Logger);
            var transition = machine.CreateTransition(machine.States[last].Name, machine.States[0].Name);
            transition.Edit();
            transition.AddTrigger(trigger);

            //transition.Succeeded += Transition_Succeeded;
            //transition.Failed += Transition_Failed;
        }

        public static void AddDoBehaviorToAll<TState>(StateMachine<TState> machine, Action<IUnityContainer> action) where TState : struct
        {
            var states = machine.States.OfType<BehavioralState>().ToList();
            foreach (BehavioralState state in states)
            {
                state.AddDoBehavior(action);
            }
        }

        public static void AddEffectToAll<TState>(StateMachine<TState> machine, string effectName, Action<IUnityContainer> effect) where TState : struct
        {
            var states = machine.States.OfType<State>().ToList();
            foreach (IState state in machine.States)
            {
                foreach (Transition t in state.Transitions.OfType<Transition>())
                {
                    t.Effect = new Behavior(effectName, effect);
                }
            }
        }
    }
}
