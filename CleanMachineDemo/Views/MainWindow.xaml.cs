using System.Windows;
using System.Windows.Input;

namespace CleanMachineDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // View first for simplicity of demo.
            //DataContext = new DemoViewModel();
            DataContext = new DemoViewModel();
        }

        //private void TriggerButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var viewModel = DataContext as DemoViewModel;
        //    viewModel.Model.TriggerAll();
        //}

        //private void ExpressionTextBox_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (Keyboard.IsKeyDown(Key.Enter))
        //    {
        //        var viewModel = DataContext as DemoViewModel;
        //        viewModel.CompileExpression();
        //    }
        //}
    }
}
