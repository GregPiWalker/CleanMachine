using System;
using System.Windows;
using System.Windows.Controls;

namespace CleanMachineDemo
{
    /// <summary>
    /// Interaction logic for StateMachineView.xaml
    /// </summary>
    [TemplatePart(Name = "PART_DiagramCanvas", Type = typeof(Canvas))]
    public partial class StateMachineView : UserControl
    {
        private Canvas _mainCanvas;

        public StateMachineView()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var canvas = FindName("PART_DiagramCanvas");
            _mainCanvas = canvas as Canvas;

            PopulateChildViews();
        }

        private void PopulateChildViews()
        {
            var vm = DataContext as StateMachineViewModel;

            foreach (Control child in _mainCanvas.Children)
            {
                if (child is StateSymbol)
                {
                    var symbol = child as StateSymbol;
                    child.DataContext = vm.CreateStateViewModel(symbol.StateName);
                }
                else if (child is TransitionSymbol)
                {
                    var symbol = child as TransitionSymbol;
                    var transitionVM = vm.CreateTransitionViewModel(symbol.SnapToState.StateName, symbol.TransitionName);
                    symbol.DataContext = transitionVM;
                    transitionVM.Failure += symbol.HandleTransitionFailure;
                    transitionVM.Success += symbol.HandleTransitionSuccess;
                }
            }
        }
    }
}
