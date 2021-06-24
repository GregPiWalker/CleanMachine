using System.Linq;
using CleanMachine.Behavioral;
using CleanMachine.Behavioral.Generic;
using CleanMachine.Generic;

namespace CleanMachineDemo
{
    public static class DemoMachineBuilder
    {
        public static MachineEditor<DemoState> BuildStateMachine(this DemoModel model, StateMachine<DemoState> machine)
        {
            var builder = MachineEditor<DemoState>.Edit(machine);
            machine.SetInitialState(DemoState.One);
            BuildStateOne(model, builder);
            BuildStateTwo(model, builder);
            BuildStateThree(model, builder);
            BuildStateFour(model, builder);

            return builder;
        }

        private static void BuildStateOne(DemoModel model, MachineEditor<DemoState> builder)
        {
            var one = builder.EditState(DemoState.One);

            // Transition from ONE to TWO
            one.TransitionTo(DemoState.Two)
                .GuardWith(() => model.OnOff, "On")
                .TriggerWithProperty(model, nameof(model.OnOff))
                .TriggerWithEvent<DemoModel, DemoEventArgs>(model, nameof(model.TriggerEvent));

            // Transition from ONE to THREE
            one.TransitionTo(DemoState.Three)
                .GuardWith(() => model.Number == 1 && !model.OnOff, "Number==1 && Off")
                .TriggerWithProperty(model, nameof(model.OnOff))
                .TriggerWithProperty(model, nameof(model.Number))
                .TriggerWithEvent<DemoModel, DemoEventArgs>(model, nameof(model.TriggerEvent));
        }

        private static void BuildStateTwo(DemoModel model, MachineEditor<DemoState> builder)
        {
            var two = builder.EditState(DemoState.Two);

            // Transition from TWO to FOUR
            var twoToFour = two.TransitionTo(DemoState.Four)
                .TriggerWithCollection(model.Observables, 5)
                .TriggerWithEvent<DemoModel, DemoEventArgs>(model, nameof(model.TriggerEvent));
        }

        private static void BuildStateThree(DemoModel model, MachineEditor<DemoState> builder)
        {
            var three = builder.EditState(DemoState.Three);

            // Transition from THREE to ONE
            var threeToOne = three.TransitionTo(DemoState.One)
                .GuardWith(() => model.Children.All(c => c.StateMachine.CurrentState == ChildState.Ready), "All Children Ready")
                .TriggerWithStateChange(model.Children.Select(c => c.StateMachine).ToList(), builder.Machine, ChildState.Ready)
                .TriggerWithEvent<DemoModel, DemoEventArgs>(model, nameof(model.TriggerEvent));
        }

        private static void BuildStateFour(DemoModel model, MachineEditor<DemoState> builder)
        {
            var four = builder.EditState(DemoState.Four);

            // Transition from FOUR to FOUR
            var fourToFour = four.TransitionTo(DemoState.Four)
                .GuardWith(() => model.LoopCount > 0, "Loop Count>0")
                .TriggerWithProperty(model, nameof(model.LoopCount))
                .TriggerWithEvent<DemoModel, DemoEventArgs>(model, nameof(model.TriggerEvent));

            // Transition from FOUR to THREE
            var fourToThree = four.TransitionTo(DemoState.Three)
                .GuardWith(() => model.BoolFunc.Invoke() && model.LoopCount == 0, "Expression Is True")
                .TriggerWithProperty(model, nameof(model.BoolFunc))
                .TriggerWithEvent<DemoModel, DemoEventArgs>(model, nameof(model.TriggerEvent));
        }
    }
}
