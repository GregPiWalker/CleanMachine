using System;
using System.Collections.Generic;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;
using log4net;
using Diversions;
using Diversions.Mvvm;
using CleanMachine.Generic;

namespace CleanMachineDemo
{
    [Diversion(MarshalOption.CurrentThread)]
    public class DemoViewModel : DivertingBindableBase
    {
        private static readonly ILog _logger = LogManager.GetLogger("Demo Logger");
        private const string DefaultStatus = "Hover over controls for tips about what they do.";
        private DemoModel _model;
        CancellationTokenSource _taskCancellationSrc;
        private string _status = DefaultStatus;

        static DemoViewModel()
        {
            // Add the option to use the UI Dispatcher.  The default diverter will still be current thread.
            // Instantiation of a DispatcherSynchronizationContext needs to happen on the main application thread in order for the correct Dispatcher value to be used.
            Diversion.AddDiverter(MarshalOption.Dispatcher, Application.Current.Dispatcher, "Invoke", new List<KeyValuePair<Type, object>>().AddKey(typeof(Delegate)).AddKey(typeof(object[])));
        }

        public DemoViewModel()
        {
            _model = new DemoModel(_logger);

            ControlPanelVM = new ControlPanelViewModel(_model);
            StateMachineVM = new StateMachineViewModel(_model, _logger);

            _model.StateMachine.StateChanged += HandleStateChanged;
            _model.ResetOccurred += HandleModelReset;
        }

        public StateMachineViewModel StateMachineVM { get; private set; }

        public ControlPanelViewModel ControlPanelVM { get; private set; }

        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        private void HandleStateChanged(object sender, StateChangedEventArgs<DemoState> e)
        {
            Status = $"State changed from {e.PreviousState} to {e.ResultingState}.";
            ResetStatus();
        }

        private void HandleModelReset(object sender, DemoEventArgs e)
        {
            Status = "Demo was reset to initial condition.";
            ResetStatus();
        }

        private void ResetStatus()
        {
            if (_taskCancellationSrc == null || _taskCancellationSrc.IsCancellationRequested)
            {
                _taskCancellationSrc = new CancellationTokenSource();
            }
            else if (_taskCancellationSrc != null)
            {
                _taskCancellationSrc.Cancel();
            }

            // Go back to default message after a wait.
            Task.Run(async () =>
            {
                await Task.Delay(10000);
                Status = DefaultStatus;
            }, _taskCancellationSrc.Token);
        }
    }
}
