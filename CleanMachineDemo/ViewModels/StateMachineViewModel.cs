using log4net;
using System.Collections.Generic;
using System.ComponentModel;

namespace CleanMachineDemo
{
    public class StateMachineViewModel
    {
        private readonly ILog _logger;
        private readonly List<StateViewModel> _states = new List<StateViewModel>();
        private readonly List<TransitionViewModel> _transitions = new List<TransitionViewModel>();

        public StateMachineViewModel(DemoModel demoModel, ILog logger)
        {
            _logger = logger;
            Model = demoModel;
        }

        public DemoModel Model { get; private set; }

        public StateViewModel CreateStateViewModel(string stateName)
        {
            var stateVM = new StateViewModel(stateName, Model.StateMachine, _logger);
            stateVM.PropertyChanged += HandleStatePropertyChanged;
            _states.Add(stateVM);
            return stateVM;
        }

        public TransitionViewModel CreateTransitionViewModel(string stateName, string transitionName)
        {
            var transitionVM = new TransitionViewModel(transitionName, Model.StateMachine, stateName, _logger);
            transitionVM.PropertyChanged += HandleTransitionPropertyChanged;
            _transitions.Add(transitionVM);
            return transitionVM;
        }

        private void HandleStatePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            var vm = sender as StateViewModel;
            if (args.PropertyName == nameof(vm.IsSelected) && vm.IsSelected)
            {
                // If a new State VM gets selected, deselect all the others.
                foreach (var state in _states)
                {
                    if (state != vm)
                    {
                        state.Deselect();
                    }
                }
            }
        }

        private void HandleTransitionPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            var vm = sender as TransitionViewModel;
            if (args.PropertyName == nameof(vm.IsSelected) && vm.IsSelected)
            {
                // If a new State VM gets selected, deselect all the others.
                foreach (var transition in _transitions)
                {
                    if (transition != vm)
                    {
                        transition.Deselect();
                    }
                }
            }
        }
    }
}
