using System;
using log4net;
using System.Windows;
using System.Threading;
using Diversions;
using Diversions.Mvvm;
using System.Collections.Generic;

namespace CleanMachineDemo
{
    [Diversion(MarshalOption.CurrentThread)]
    public class DemoViewModel : DivertingBindableBase
    {
        private static readonly ILog _logger = LogManager.GetLogger("Demo Logger");
        private DemoModel _model;

        static DemoViewModel()
        {
            // Add the option to use the UI Dispatcher.  The default diverter will still be current thread.
            Diversion.AddDiverter(MarshalOption.Dispatcher, Application.Current.Dispatcher, "Invoke", new List<KeyValuePair<Type, object>>().AddKey(typeof(Delegate)).AddKey(typeof(object[])), SynchronizationContext.Current);
        }

        public DemoViewModel()
        {
            _model = new DemoModel(_logger);

            StateMachineVM = new StateMachineViewModel(_model, _logger);
            ControlPanelVM = new ControlPanelViewModel(_model);
        }

        public StateMachineViewModel StateMachineVM { get; private set; }

        public ControlPanelViewModel ControlPanelVM { get; private set; }
    }
}
