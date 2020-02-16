using Microsoft.CodeAnalysis.CSharp.Scripting;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.Collections;
using Prism.Commands;

namespace CleanMachineDemo
{
    public class ControlPanelViewModel : BindableBase, INotifyDataErrorInfo
    {
        private const string LambdaOperator = "()=>";
        private string _expression;
        private bool _hasErrors;

        public ControlPanelViewModel(DemoModel demoModel)
        {
            Model = demoModel;
            TriggerAllCommand = new DelegateCommand(Model.TriggerAll);

            // Compile a dummy expression in order to front load all the roslyn dependencies.
            _expression = "false";
            CompileExpression();
            _expression = string.Empty;

            Model.PropertyChanged += HandleModelPropertyChanged;
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public DemoModel Model { get; private set; }

        public string Expression
        {
            get { return _expression; }
            set { SetProperty(ref _expression, value, nameof(Expression)); }
        }

        public DelegateCommand TriggerAllCommand { get; private set; }

        public bool HasErrors
        {
            get { return _hasErrors; }
            set
            {
                if (value == _hasErrors)
                {
                    return;
                }

                _hasErrors = value;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Expression)));
            }
        }

        public void CompileExpression()
        {
            lock (_expression)
            {
                CompileExpressionAsync();
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName == nameof(Expression))
            {
                return new object[] { new Exception("Compiler Exception: Invalid boolean expression", null) };
            }

            return new object[0];
        }

        private async void CompileExpressionAsync()
        {
            try
            {
                Model.BoolFunc = await CSharpScript.EvaluateAsync<Func<bool>>(LambdaOperator + _expression);
                HasErrors = false;
            }
            catch
            {
                Model.BoolFunc = () => false;
                HasErrors = true;
            }
        }

        private void ResetExpression()
        {
            _expression = "false";
            CompileExpression();
            Expression = string.Empty;
        }

        private void HandleModelPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(Model.BoolFunc) && Model.BoolFunc == null)
            {
                ResetExpression();
            }
        }
    }
}
