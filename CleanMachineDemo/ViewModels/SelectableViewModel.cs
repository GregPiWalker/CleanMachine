using log4net;
using Prism.Mvvm;

namespace CleanMachineDemo
{
    public class SelectableViewModel : BindableBase
    {
        protected readonly ILog _logger;
        private bool _isSelected;

        public SelectableViewModel(ILog logger)
        {
            _logger = logger;
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value, nameof(IsSelected)); }
        }

        public void Select()
        {
            IsSelected = true;
        }

        public void Deselect()
        {
            IsSelected = false;
        }

        public virtual void LogDiagnostics()
        {
            _logger.Debug($"IsSelected: {IsSelected}");
            _logger.Debug($"------------------- End Diagnostics -------------------");
        }
    }
}
