﻿using System.Timers;
using CleanMachine.Generic;
using CleanMachine.Behavioral;
using CleanMachine.Behavioral.Generic;

namespace CleanMachineDemo
{
    public static class ChildMachineBuilder
    {
        public static MachineEditor<ChildState> BuildStateMachine(this ChildModel model, StateMachine<ChildState> machine)
        {
            var builder = MachineEditor<ChildState>.Edit(machine);
            machine.SetInitialState(ChildState.Ready);
            BuildReadyState(model, builder);
            BuildBusyState(model, builder);

            return builder;
        }

        private static void BuildReadyState(ChildModel model, MachineEditor<ChildState> builder)
        {
            var ready = builder.EditState(ChildState.Ready);

            // Transition from READY to BUSY
            var readyToBusy = ready.TransitionTo(ChildState.Busy)
                .TriggerWithEvent<Timer, ElapsedEventHandler, ElapsedEventArgs>(model.RandomTimer, nameof(model.RandomTimer.Elapsed))
                .HaveEffect("Run Timer", (di) => model.RunTimer());
        }

        private static void BuildBusyState(ChildModel model, MachineEditor<ChildState> builder)
        {
            var busy = builder.EditState(ChildState.Busy);

            // Transition from BUSY to READY
            var busyToReady = busy.TransitionTo(ChildState.Ready)
                .TriggerWithEvent<Timer, ElapsedEventHandler, ElapsedEventArgs>(model.RandomTimer, nameof(model.RandomTimer.Elapsed))
                .HaveEffect("Run Timer", (di) => model.RunTimer());
        }
    }
}
