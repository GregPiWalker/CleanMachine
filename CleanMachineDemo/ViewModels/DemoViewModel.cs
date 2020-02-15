using System;
using Prism.Mvvm;
using log4net;

namespace CleanMachineDemo
{
    public class DemoViewModel : BindableBase
    {
        private static readonly ILog _logger = LogManager.GetLogger("Demo Logger");
        private DemoModel _model;

        public DemoViewModel()
        {
            _model = new DemoModel(_logger);

            StateMachineVM = new StateMachineViewModel(_model);
            ControlPanelVM = new ControlPanelViewModel(_model);
        }

        public StateMachineViewModel StateMachineVM { get; private set; }

        public ControlPanelViewModel ControlPanelVM { get; private set; }
    }
}
