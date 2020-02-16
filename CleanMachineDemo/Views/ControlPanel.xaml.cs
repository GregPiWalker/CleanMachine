using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CleanMachineDemo
{
    /// <summary>
    /// Interaction logic for ControlPanel.xaml
    /// </summary>
    public partial class ControlPanel : UserControl
    {
        public ControlPanel()
        {
            InitializeComponent();
        }

        private void ExpressionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                var viewModel = DataContext as ControlPanelViewModel;
                viewModel?.CompileExpression();
            }
        }
    }
}
