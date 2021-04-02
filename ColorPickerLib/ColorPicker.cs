//
// ColorPicker.cs 
// An HSB (hue, saturation, brightness) based
// color picker.
//
// 
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Samples.CustomControls
{
    #region ColorPicker

    public class ColorPicker : Control
    {
        #region Private Fields

        private SpectrumSlider _colorSlider;
        private static readonly string s_colorSliderName = "PART_ColorSlider";
        private FrameworkElement _colorDetail;
        private static readonly string s_colorDetailName = "PART_ColorDetail";
        private readonly TranslateTransform _markerTransform = new();
        private Path _colorMarker;
        private static readonly string s_colorMarkerName = "PART_ColorMarker";
        private Point? _colorPosition;
        private Color _color;
        private bool _shouldFindPoint;
        private bool _templateApplied;
        private bool _isAlphaChange;

        #endregion

        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        public ColorPicker()
        {
            _templateApplied = false;
            _color = Colors.White;
            _shouldFindPoint = true;
            SetValue(AProperty, _color.A);
            SetValue(RProperty, _color.R);
            SetValue(GProperty, _color.G);
            SetValue(BProperty, _color.B);
            SetValue(SelectedColorProperty, _color);
        }


        #region Public Methods

        public override void OnApplyTemplate()
        {

            base.OnApplyTemplate();
            _colorDetail = GetTemplateChild(s_colorDetailName) as FrameworkElement;
            _colorMarker = GetTemplateChild(s_colorMarkerName) as Path;
            _colorSlider = GetTemplateChild(s_colorSliderName) as SpectrumSlider;
            _colorSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(BaseColorChanged);


            _colorMarker.RenderTransform = _markerTransform;
            _colorMarker.RenderTransformOrigin = new Point(0.5, 0.5);
            _colorDetail.MouseLeftButtonDown += new MouseButtonEventHandler(OnMouseLeftButtonDown);
            _colorDetail.PreviewMouseMove += new MouseEventHandler(OnMouseMove);
            _colorDetail.SizeChanged += new SizeChangedEventHandler(ColorDetailSizeChanged);

            _templateApplied = true;
            _shouldFindPoint = true;
            _isAlphaChange = false;

            SelectedColor = _color;
        }

        #endregion


        #region Public Properties

        // Gets or sets the selected color.
        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set
            {
                SetValue(SelectedColorProperty, _color);
                SetColor(value);
            }
        }


        #region RGB Properties
        // Gets or sets the ARGB alpha value of the selected color.
        public byte A
        {
            get => (byte)GetValue(AProperty);
            set => SetValue(AProperty, value);
        }

        // Gets or sets the ARGB red value of the selected color.
        public byte R
        {
            get => (byte)GetValue(RProperty);
            set => SetValue(RProperty, value);
        }

        // Gets or sets the ARGB green value of the selected color.
        public byte G
        {
            get => (byte)GetValue(GProperty);
            set => SetValue(GProperty, value);
        }

        // Gets or sets the ARGB blue value of the selected color.
        public byte B
        {
            get => (byte)GetValue(BProperty);
            set => SetValue(BProperty, value);
        }
        #endregion RGB Properties

        #region ScRGB Properties

        // Gets or sets the ScRGB alpha value of the selected color.
        public double ScA
        {
            get => (double)GetValue(ScAProperty);
            set => SetValue(ScAProperty, value);
        }

        // Gets or sets the ScRGB red value of the selected color.
        public double ScR
        {
            get => (double)GetValue(ScRProperty);
            set => SetValue(RProperty, value);
        }

        // Gets or sets the ScRGB green value of the selected color.
        public double ScG
        {
            get => (double)GetValue(ScGProperty);
            set => SetValue(GProperty, value);
        }

        // Gets or sets the ScRGB blue value of the selected color.
        public double ScB
        {
            get => (double)GetValue(BProperty);
            set => SetValue(BProperty, value);
        }
        #endregion ScRGB Properties

        // Gets or sets the the selected color in hexadecimal notation.
        public string HexadecimalString
        {
            get => (string)GetValue(HexadecimalStringProperty);
            set => SetValue(HexadecimalStringProperty, value);
        }

        #endregion


        #region Public Events

        public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged
        {
            add
            {
                AddHandler(SelectedColorChangedEvent, value);
            }

            remove
            {
                RemoveHandler(SelectedColorChangedEvent, value);
            }
        }

        #endregion


        #region Dependency Property Fields
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPicker),
                new PropertyMetadata(Colors.Transparent, SelectedColor_Changed));

        public static readonly DependencyProperty ScAProperty =
            DependencyProperty.Register("ScA", typeof(float), typeof(ColorPicker),
                new PropertyMetadata((float)1, ScAChanged));

        public static readonly DependencyProperty ScRProperty =
            DependencyProperty.Register("ScR", typeof(float), typeof(ColorPicker),
                new PropertyMetadata((float)1, ScRChanged));

        public static readonly DependencyProperty ScGProperty =
            DependencyProperty.Register("ScG", typeof(float), typeof(ColorPicker),
                new PropertyMetadata((float)1, ScGChanged));

        public static readonly DependencyProperty ScBProperty =
            DependencyProperty.Register("ScB", typeof(float), typeof(ColorPicker),
                new PropertyMetadata((float)1, ScBChanged));

        public static readonly DependencyProperty AProperty =
            DependencyProperty.Register("A", typeof(byte), typeof(ColorPicker),
                new PropertyMetadata((byte)255, AChanged));

        public static readonly DependencyProperty RProperty =
            DependencyProperty.Register("R", typeof(byte), typeof(ColorPicker),
                new PropertyMetadata((byte)255, RChanged));

        public static readonly DependencyProperty GProperty =
            DependencyProperty.Register("G", typeof(byte), typeof(ColorPicker),
                new PropertyMetadata((byte)255, GChanged));

        public static readonly DependencyProperty BProperty =
            DependencyProperty.Register("B", typeof(byte), typeof(ColorPicker),
                new PropertyMetadata((byte)255, BChanged));

        public static readonly DependencyProperty HexadecimalStringProperty =
            DependencyProperty.Register("HexadecimalString", typeof(string), typeof(ColorPicker),
                new PropertyMetadata("#FFFFFFFF", HexadecimalStringChanged));

        #endregion


        #region RoutedEvent Fields

        public static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent(
            "SelectedColorChanged",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<Color>),
            typeof(ColorPicker)
        );
        #endregion


        #region Property Changed Callbacks

        private static void AChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnAChanged((byte)e.NewValue);
        }

        protected virtual void OnAChanged(byte newValue)
        {
            _color.A = newValue;
            SetValue(ScAProperty, _color.ScA);
            SetValue(SelectedColorProperty, _color);
        }

        private static void RChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnRChanged((byte)e.NewValue);
        }

        protected virtual void OnRChanged(byte newValue)
        {
            _color.R = newValue;
            SetValue(ScRProperty, _color.ScR);
            SetValue(SelectedColorProperty, _color);
        }


        private static void GChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnGChanged((byte)e.NewValue);
        }

        protected virtual void OnGChanged(byte newValue)
        {
            _color.G = newValue;
            SetValue(ScGProperty, _color.ScG);
            SetValue(SelectedColorProperty, _color);
        }


        private static void BChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnBChanged((byte)e.NewValue);
        }

        protected virtual void OnBChanged(byte newValue)
        {
            _color.B = newValue;
            SetValue(ScBProperty, _color.ScB);
            SetValue(SelectedColorProperty, _color);
        }

        private static void ScAChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnScAChanged((float)e.NewValue);
        }

        protected virtual void OnScAChanged(float newValue)
        {
            _isAlphaChange = true;
            if (_shouldFindPoint)
            {
                _color.ScA = newValue;
                SetValue(AProperty, _color.A);
                SetValue(SelectedColorProperty, _color);
                SetValue(HexadecimalStringProperty, _color.ToString());
            }
            _isAlphaChange = false;
        }

        private static void ScRChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnScRChanged((float)e.NewValue);
        }

        protected virtual void OnScRChanged(float newValue)
        {
            if (_shouldFindPoint)
            {
                _color.ScR = newValue;
                SetValue(RProperty, _color.R);
                SetValue(SelectedColorProperty, _color);
                SetValue(HexadecimalStringProperty, _color.ToString());
            }
        }

        private static void ScGChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnScGChanged((float)e.NewValue);
        }

        protected virtual void OnScGChanged(float newValue)
        {
            if (_shouldFindPoint)
            {
                _color.ScG = newValue;
                SetValue(GProperty, _color.G);
                SetValue(SelectedColorProperty, _color);
                SetValue(HexadecimalStringProperty, _color.ToString());
            }
        }

        private static void ScBChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnScBChanged((float)e.NewValue);
        }

        protected virtual void OnScBChanged(float newValue)
        {
            if (_shouldFindPoint)
            {
                _color.ScB = newValue;
                SetValue(BProperty, _color.B);
                SetValue(SelectedColorProperty, _color);
                SetValue(HexadecimalStringProperty, _color.ToString());
            }
        }

        private static void HexadecimalStringChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        {
            var c = (ColorPicker)d;
            c.OnHexadecimalStringChanged((string)e.OldValue, (string)e.NewValue);
        }

        protected virtual void OnHexadecimalStringChanged(string oldValue, string newValue)
        {
            try
            {
                if (_shouldFindPoint)
                {
                    _color = (Color)ColorConverter.ConvertFromString(newValue);
                }

                SetValue(AProperty, _color.A);
                SetValue(RProperty, _color.R);
                SetValue(GProperty, _color.G);
                SetValue(BProperty, _color.B);

                if (_shouldFindPoint && !_isAlphaChange && _templateApplied)
                {
                    UpdateMarkerPosition(_color);
                }
            }
            catch (FormatException)
            {
                SetValue(HexadecimalStringProperty, oldValue);
            }

        }

        private static void SelectedColor_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cPicker = (ColorPicker)d;
            cPicker.OnSelectedColorChanged((Color)e.OldValue, (Color)e.NewValue);
        }

        protected virtual void OnSelectedColorChanged(Color oldColor, Color newColor)
        {

            var newEventArgs =
                new RoutedPropertyChangedEventArgs<Color>(oldColor, newColor)
                {
                    RoutedEvent = SelectedColorChangedEvent
                };
            RaiseEvent(newEventArgs);
        }

        #endregion


        #region Template Part Event Handlers

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            _templateApplied = false;
            if (oldTemplate != null)
            {
                _colorSlider.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(BaseColorChanged);
                _colorDetail.MouseLeftButtonDown -= new MouseButtonEventHandler(OnMouseLeftButtonDown);
                _colorDetail.PreviewMouseMove -= new MouseEventHandler(OnMouseMove);
                _colorDetail.SizeChanged -= new SizeChangedEventHandler(ColorDetailSizeChanged);
                _colorDetail = null;
                _colorMarker = null;
                _colorSlider = null;
            }
            base.OnTemplateChanged(oldTemplate, newTemplate);
        }

        private void BaseColorChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            if (_colorPosition != null)
            {

                DetermineColor((Point)_colorPosition);
            }

        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            var p = e.GetPosition(_colorDetail);
            UpdateMarkerPosition(p);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {

                var p = e.GetPosition(_colorDetail);
                UpdateMarkerPosition(p);
                Mouse.Synchronize();

            }
        }

        private void ColorDetailSizeChanged(object sender, SizeChangedEventArgs args)
        {
            if (args.PreviousSize != Size.Empty &&
                args.PreviousSize.Width != 0 && args.PreviousSize.Height != 0)
            {
                double widthDifference = args.NewSize.Width / args.PreviousSize.Width;
                double heightDifference = args.NewSize.Height / args.PreviousSize.Height;
                _markerTransform.X *= widthDifference;
                _markerTransform.Y *= heightDifference;
            }
            else if (_colorPosition != null)
            {
                _markerTransform.X = ((Point)_colorPosition).X * args.NewSize.Width;
                _markerTransform.Y = ((Point)_colorPosition).Y * args.NewSize.Height;
            }
        }

        #endregion


        #region Color Resolution Helpers

        private void SetColor(Color theColor)
        {
            _color = theColor;

            if (_templateApplied)
            {
                SetValue(AProperty, _color.A);
                SetValue(RProperty, _color.R);
                SetValue(GProperty, _color.G);
                SetValue(BProperty, _color.B);
                UpdateMarkerPosition(theColor);
            }
        }

        private void UpdateMarkerPosition(Point p)
        {
            _markerTransform.X = p.X;
            _markerTransform.Y = p.Y;
            p.X /= _colorDetail.ActualWidth;
            p.Y /= _colorDetail.ActualHeight;
            _colorPosition = p;
            DetermineColor(p);
        }

        private void UpdateMarkerPosition(Color theColor)
        {
            _colorPosition = null;

            var hsv = HsvColor.FromRgbColor(theColor);

            _colorSlider.Value = hsv.H;

            var p = new Point(hsv.S, 1 - hsv.V);

            _colorPosition = p;
            p.X *= _colorDetail.ActualWidth;
            p.Y *= _colorDetail.ActualHeight;
            _markerTransform.X = p.X;
            _markerTransform.Y = p.Y;
        }

        private void DetermineColor(Point p)
        {
            _color = new HsvColor(360 - _colorSlider.Value, p.X, 1 - p.Y).ToRgbColor();
            _shouldFindPoint = false;
            _color.ScA = (float)GetValue(ScAProperty);
            SetValue(HexadecimalStringProperty, _color.ToString());
            _shouldFindPoint = true;
        }

        #endregion

    }

    #endregion ColorPicker


}