using CleanMachine;
using CleanMachine.Generic;
using System;

namespace CleanMachineDemo
{
    public class TransitionViewModel : SelectableViewModel
    {
        private string _transitionName;

        public TransitionViewModel(string transitionName, StateMachine<DemoState> machine, string stateName)
        {
            _transitionName = transitionName;
            var state = (DemoState)Enum.Parse(typeof(DemoState), stateName);
            machine[state].TransitionSucceeded += HandleTransitionSucceeded;
            machine[state].TransitionFailed += HandleTransitionFailed;
        }

        private void HandleTransitionSucceeded(object sender, CleanMachine.Interfaces.TransitionEventArgs args)
        {
            //TODO: this name match is not unique enough
            if (_transitionName == args.Transition.Name)
            {
                Select();
            }
        }

        private void HandleTransitionFailed(object sender, CleanMachine.Interfaces.TransitionEventArgs args)
        {
            //TODO: this name match is not unique enough
            if (_transitionName == args.Transition.Name)
            {
                //Select();
            }
        }
    }
}
