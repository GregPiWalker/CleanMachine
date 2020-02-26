using CleanMachine.Generic;
using log4net;
using System;

namespace CleanMachineDemo
{
    public class TransitionViewModel : SelectableViewModel
    {
        private string _transitionName;

        public TransitionViewModel(string transitionName, BehavioralStateMachine<DemoState> machine, string stateName, ILog logger)
            : base(logger)
        {
            _transitionName = transitionName;
            var state = (DemoState)Enum.Parse(typeof(DemoState), stateName);
            machine[state].TransitionSucceeded += HandleTransitionSucceeded;
            machine[state].TransitionFailed += HandleTransitionFailed;
        }

        public event EventHandler Failure;
        public event EventHandler Success;

        public override void LogDiagnostics()
        {
            _logger.Debug(string.Empty);
            _logger.Debug($"------------------- Diagnostics: Transition '{_transitionName}' -------------------");
            base.LogDiagnostics();
        }

        private void HandleTransitionSucceeded(object sender, CleanMachine.Interfaces.TransitionEventArgs args)
        {
            //TODO: this name match is not unique enough
            if (_transitionName != args.Transition.Name)
            {
                return;
            }

            Select();
            
            //TODO: invoke on dispatcher
            Success?.Invoke(this, new EventArgs());
        }

        private void HandleTransitionFailed(object sender, CleanMachine.Interfaces.TransitionEventArgs args)
        {
            //TODO: this name match is not unique enough
            if (_transitionName != args.Transition.Name)
            {
                return;
            }

            //TODO: invoke on dispatcher
            Failure?.Invoke(this, new EventArgs());
        }
    }
}
