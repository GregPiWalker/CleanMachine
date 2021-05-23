using log4net;
using System;
using CleanMachine.Generic;
using Diversions;

namespace CleanMachineDemo
{
    [Diversion(MarshalOption.CurrentThread)]
    public class TransitionViewModel : SelectableViewModel
    {
        private readonly DiversionDelegate<EventArgs> _success = new DiversionDelegate<EventArgs>();
        private readonly DiversionDelegate<EventArgs> _failure = new DiversionDelegate<EventArgs>();
        private string _transitionName;

        public TransitionViewModel(string transitionName, StateMachine<DemoState> machine, string stateName, ILog logger)
            : base(logger)
        {
            _transitionName = transitionName;
            var state = (DemoState)Enum.Parse(typeof(DemoState), stateName);
            machine[state].TransitionSucceeded += HandleTransitionSucceeded;
            machine[state].TransitionFailed += HandleTransitionFailed;
        }

        public event EventHandler Failure
        {
            add { _failure.Add(value); }
            remove { _failure.Remove(value); }
        }

        public event EventHandler Success
        {
            add { _success.Add(value); }
            remove { _success.Remove(value); }
        }

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
            
            _success.Invoke(this, new EventArgs());
        }

        private void HandleTransitionFailed(object sender, CleanMachine.Interfaces.TransitionEventArgs args)
        {
            //TODO: this name match is not unique enough
            if (_transitionName != args.Transition.Name)
            {
                return;
            }
            
            _failure.Invoke(this, new EventArgs());
        }
    }
}
