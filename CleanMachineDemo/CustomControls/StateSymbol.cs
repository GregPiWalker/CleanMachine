using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Diversions;

namespace CleanMachineDemo.CustomControls
{
    [Diversion(MarshalOption.Dispatcher)]
    [TemplatePart(Name = "PART_RectangleFrame", Type = typeof(Path))]
    public class StateSymbol : Control
    {
        public static readonly DependencyProperty StateNameProperty =
            DependencyProperty.Register("StateName", typeof(string), typeof(StateSymbol));
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(StateSymbol));
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(StateSymbol));

        private StateViewModel _viewModel = null;

        public StateSymbol()
        {
            // Programmatically add the style for this control to it's resource dictionary.
            var styles = new ResourceDictionary();
            // Must use the "pack" URI syntax or else XAML Designer won't find it.
            string assemblyName = "CleanMachineDemo";
            string resourcePath = System.IO.Path.Combine("CustomControls", GetType().Name);
            styles.Source = new Uri($"pack://application:,,,/{assemblyName};component/{resourcePath}.xaml", UriKind.RelativeOrAbsolute);
            Resources.MergedDictionaries.Add(styles);

            DefaultStyleKey = typeof(StateSymbol);
        }

        public string StateName
        {
            get { return GetValue(StateNameProperty) as string; }
            set { SetValue(StateNameProperty, value); }
        }

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_viewModel == null)
            {
                _viewModel = DataContext as StateViewModel;
            }

            var rectangle = (Template.FindName("PART_RectangleFrame", this) as Path).Data as RectangleGeometry;
            var rect = rectangle.Rect;
            if (double.IsNaN(Width))
            {
                Width = rect.Width;
            }
            else
            {
                rect.Width = Width;
            }

            if (double.IsNaN(Height))
            {
                Height = rect.Height;
            }
            else
            {
                rect.Height = Height;
            }

            rectangle.Rect = rect;

        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            //if (_viewModel == null)
            //{
            //    _viewModel = DataContext as StateViewModel;
            //    if (_viewModel == null)
            //    {
            //        return;
            //    }
            //}

            _viewModel.LogDiagnostics();
        }

        public void Select()
        {
            if (_viewModel == null)
            {
                _viewModel = DataContext as StateViewModel;
                if (_viewModel == null)
                {
                    return;
                }
            }

            _viewModel.Select();
        }

        public void Deselect()
        {
            if (_viewModel == null)
            {
                _viewModel = DataContext as StateViewModel;
                if (_viewModel == null)
                {
                    return;
                }
            }

            _viewModel.Deselect();
        }
    }
}
