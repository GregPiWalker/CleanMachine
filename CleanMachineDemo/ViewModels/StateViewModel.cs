using CleanMachine.Generic;
using CleanMachine.Interfaces;
using log4net;
using System;

namespace CleanMachineDemo
{
    public class StateViewModel : SelectableViewModel
    {
        private DemoState _myState;

        public StateViewModel(string stateName, StateMachine<DemoState> machine, ILog logger)
            : base(logger)
        {
            _myState = (DemoState)Enum.Parse(typeof(DemoState), stateName);
            Model = machine[_myState];
            Model.EntryInitiated += HandleEnteringState;

            // Check for initial state.
            if (Model.IsCurrentState)
            {
                Select();
            }
        }

        public IState Model { get; private set; }

        public override void LogDiagnostics()
        {
            _logger.Debug(string.Empty);
            _logger.Debug($"------------------- Diagnostics: State '{_myState}' -------------------");
            Model.LogDiagnostics();
            base.LogDiagnostics();
        }

        private void HandleEnteringState(object sender, StateEnteredEventArgs args)
        {
            Select();
        }
    }
}
