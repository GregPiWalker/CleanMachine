using CleanMachine.Generic;
using CleanMachine.Interfaces;
using System;

namespace CleanMachineDemo
{
    public class StateViewModel : SelectableViewModel
    {
        private DemoState _myState;

        public StateViewModel(string stateName, StateMachine<DemoState> machine)
        {
            _myState = (DemoState)Enum.Parse(typeof(DemoState), stateName);
            var state = machine[_myState];
            state.EntryInitiated += HandleEnteringState;

            // Check for initial state.
            if (state.IsCurrentState)
            {
                Select();
            }
        }

        private void HandleEnteringState(object sender, StateEnteredEventArgs args)
        {
            Select();
        }
    }
}
