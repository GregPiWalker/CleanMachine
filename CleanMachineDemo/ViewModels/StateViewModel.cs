﻿using log4net;
using System;
using CleanMachine.Interfaces;
using CleanMachine.Generic;
using Diversions;

namespace CleanMachineDemo
{
    [Diversion(MarshalOption.CurrentThread)]
    public class StateViewModel : SelectableViewModel
    {
        private DemoState _myState;

        public StateViewModel(string stateName, StateMachine<DemoState> machine, ILog logger)
            : base(logger)
        {
            _myState = (DemoState)Enum.Parse(typeof(DemoState), stateName);
            Model = machine[_myState];
            (Model as IStateBehavior).EntryInitiated += HandleEnteringState;

            // Check for initial state.
            if (Model.IsCurrentState)
            {
                Select();
            }
        }

        public event EventHandler<string> Message;

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
