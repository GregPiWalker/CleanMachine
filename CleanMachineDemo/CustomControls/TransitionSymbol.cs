using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CleanMachineDemo
{
    [TemplatePart(Name = "PART_ArrowShaft", Type = typeof(Path))]
    public class TransitionSymbol : Control
    {
        private const double ArrowheadHeight = 10d;
        private const double ArrowheadOverlap = 2d;
        private TransitionViewModel _viewModel;
        private Path _linePath;

        public static readonly DependencyProperty TransitionNameProperty =
            DependencyProperty.Register("TransitionName", typeof(string), typeof(TransitionSymbol));
        public static readonly DependencyProperty SnapToStateProperty =
            DependencyProperty.Register("SnapToState", typeof(StateSymbol), typeof(TransitionSymbol));
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(TransitionSymbol));
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(TransitionSymbol));
        public static readonly DependencyProperty TransformOriginProperty =
            DependencyProperty.Register("TransformOrigin", typeof(Point), typeof(TransitionSymbol));
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(TransitionSymbol));
        public static readonly DependencyProperty ArrowheadAngleProperty =
            DependencyProperty.Register("ArrowheadAngle", typeof(double), typeof(TransitionSymbol));
        public static readonly DependencyProperty ArrowheadTransXProperty =
            DependencyProperty.Register("ArrowheadTransX", typeof(double), typeof(TransitionSymbol));
        public static readonly DependencyProperty ArrowheadTransYProperty =
            DependencyProperty.Register("ArrowheadTransY", typeof(double), typeof(TransitionSymbol));
        public static readonly DependencyProperty IsRecursiveProperty =
            DependencyProperty.Register("IsRecursive", typeof(bool), typeof(TransitionSymbol));

        public TransitionSymbol()
        {
            var styles = new ResourceDictionary();
            styles.Source = new Uri($";component/CustomControls/{GetType().Name}.xaml", UriKind.RelativeOrAbsolute);
            Resources.MergedDictionaries.Add(styles);
        }

        public string TransitionName
        {
            get { return GetValue(TransitionNameProperty) as string; }
            set { SetValue(TransitionNameProperty, value); }
        }

        public StateSymbol SnapToState
        {
            get { return GetValue(SnapToStateProperty) as StateSymbol; }
            set { SetValue(SnapToStateProperty, value); }
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

        public Point TransformOrigin
        {
            get { return (Point)GetValue(TransformOriginProperty); }
            set { SetValue(TransformOriginProperty, value); }
        }

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public double ArrowheadTransX
        {
            get { return (double)GetValue(ArrowheadTransXProperty); }
            set { SetValue(ArrowheadTransXProperty, value); }
        }

        public double ArrowheadTransY
        {
            get { return (double)GetValue(ArrowheadTransYProperty); }
            set { SetValue(ArrowheadTransYProperty, value); }
        }

        public double ArrowheadAngle
        {
            get { return (double)GetValue(ArrowheadAngleProperty); }
            set { SetValue(ArrowheadAngleProperty, value); }
        }

        public bool IsRecursive
        {
            get { return (bool)GetValue(IsRecursiveProperty); }
            set { SetValue(IsRecursiveProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _linePath = Template.FindName("PART_ArrowShaft", this) as Path;
            if (IsRecursive)
            {
                ApplyCurvedLineStyle();
            }
            else
            {
                ApplyStraightLineStyle();
            }
        }

        public void Select()
        {
            if (_viewModel == null)
            {
                _viewModel = DataContext as TransitionViewModel;
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
                _viewModel = DataContext as TransitionViewModel;
                if (_viewModel == null)
                {
                    return;
                }
            }

            _viewModel.Deselect();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Select();
        }

        private void ApplyStraightLineStyle()
        {
            var template = Template.Resources["StraightLine"] as LineGeometry;
            var end = template.EndPoint;
            var length = end.Y - template.StartPoint.Y;

            var line = new LineGeometry() { StartPoint = template.StartPoint };

            if (double.IsNaN(Height))
            {
                Height = length + ArrowheadOverlap;
            }
            else
            {
                end.Y = Height - ArrowheadOverlap;
            }

            line.EndPoint = end;
            _linePath.Data = line;

            ArrowheadTransY = Height - ArrowheadHeight - ArrowheadOverlap;
            ArrowheadTransX = 0;
            ArrowheadAngle = 0;

            TransformOrigin = new Point(0.5, 0);
        }

        private void ApplyCurvedLineStyle()
        {
            var curve = Template.Resources["CurvedLine"] as PathGeometry;
            _linePath.Data = curve;

            ArrowheadTransY = 23.2;
            ArrowheadTransX = 10.9;
            ArrowheadAngle = 110;

            TransformOrigin = new Point(0,0);
        }
    }
}
