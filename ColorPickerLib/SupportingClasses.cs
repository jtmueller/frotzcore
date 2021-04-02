//
// SupportingClasses.cs 
//
// 
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Samples.CustomControls
{

    #region SpectrumSlider

    public class SpectrumSlider : Slider
    {
        static SpectrumSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumSlider),
                new FrameworkPropertyMetadata(typeof(SpectrumSlider)));
        }

        #region Private Fields
        private static readonly string s_spectrumDisplayName = "PART_SpectrumDisplay";
        private Rectangle _spectrumDisplay;
        private LinearGradientBrush _pickerBrush;
        #endregion

        #region Public Properties
        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }
        #endregion 


        #region Dependency Property Fields
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(SpectrumSlider),
                new PropertyMetadata(System.Windows.Media.Colors.Transparent));

        #endregion


        #region Public Methods

        public override void OnApplyTemplate()
        {

            base.OnApplyTemplate();
            _spectrumDisplay = GetTemplateChild(s_spectrumDisplayName) as Rectangle;
            UpdateColorSpectrum();
            OnValueChanged(double.NaN, Value);

        }

        #endregion


        #region Protected Methods
        protected override void OnValueChanged(double oldValue, double newValue)
        {

            base.OnValueChanged(oldValue, newValue);
            var theColor = new HsvColor(360 - newValue, 1, 1).ToRgbColor();
            SetValue(SelectedColorProperty, theColor);
        }
        #endregion


        #region Private Methods

        private void UpdateColorSpectrum()
        {
            if (_spectrumDisplay != null)
            {
                CreateSpectrum();
            }
        }

        private void CreateSpectrum()
        {

            _pickerBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation
            };


            var colors = ColorUtilities.GenerateHsvSpectrum();
            double stopIncrement = (double)1 / colors.Length;

            int i;
            for (i = 0; i < colors.Length; i++)
            {
                _pickerBrush.GradientStops.Add(new GradientStop(colors[i], i * stopIncrement));
            }

            _pickerBrush.GradientStops[i - 1].Offset = 1.0;
            _spectrumDisplay.Fill = _pickerBrush;

        }
        #endregion

    }

    #endregion SpectrumSlider


    #region ColorUtilities

    internal static class ColorUtilities
    {
        /// <summary>
        /// Generates a list of colors with hues ranging from 0 360
        /// and a saturation and value of 1. 
        /// </summary>
        public static Color[] GenerateHsvSpectrum()
        {
            var colors = new Color[30];

            for (int i = 0; i < 29; i++)
            {
                colors[i] = new HsvColor(i * 12, 1, 1).ToRgbColor();
            }
            colors[29] = new HsvColor(0, 1, 1).ToRgbColor();

            return colors;
        }
    }

    #endregion ColorUtilities


    // Describes a color in terms of
    // Hue, Saturation, and Value (brightness)
    #region HsvColor
    internal readonly struct HsvColor : IEquatable<HsvColor>
    {
        public readonly double H;
        public readonly double S;
        public readonly double V;

        public HsvColor(double h, double s, double v)
        {
            H = h;
            S = s;
            V = v;
        }

        public void Deconstruct(out double h, out double s, out double v)
        {
            h = H;
            s = S;
            v = V;
        }

        public bool Equals(HsvColor other) => other.H == H && other.S == S && other.V == V;

        public override bool Equals(object obj) => obj is HsvColor other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(H, S, V);

        /// <summary>
        /// Converts an HSV color to an RGB color.
        /// </summary>
        public Color ToRgbColor()
        {
            double h = H;
            double s = S;
            double v = V;
            double r, g, b;

            if (s == 0)
            {
                r = v;
                g = v;
                b = v;
            }
            else
            {
                int i;
                double f, p, q, t;

                h = h == 360 ? 0 : h / 60;

                i = (int)Math.Truncate(h);
                f = h - i;

                p = v * (1.0 - s);
                q = v * (1.0 - (s * f));
                t = v * (1.0 - (s * (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = v;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = v;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = v;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = v;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = v;
                        break;

                    default:
                        r = v;
                        g = p;
                        b = q;
                        break;
                }

            }

            return Color.FromArgb(0xff, (byte)(r * 0xff), (byte)(g * 0xff), (byte)(b * 0xff));
        }

        /// <summary>
        /// Converts an RGB color to an HSV color.
        /// </summary>
        public static HsvColor FromRgbColor(Color color)
            => FromRgb(color.R, color.G, color.B);

        /// <summary>
        /// Converts an RGB color to an HSV color.
        /// </summary>
        /// <param name="r">Red</param>
        /// <param name="g">Green</param>
        /// <param name="b">Blue</param>
        public static HsvColor FromRgb(int r, int g, int b)
        {
            double delta, min;
            double h = 0, s, v;

            min = Math.Min(Math.Min(r, g), b);
            v = Math.Max(Math.Max(r, g), b);
            delta = v - min;

            s = v == 0.0 ? 0 : delta / v;

            if (s == 0)
            {
                h = 0.0;
            }
            else
            {
                if (r == v)
                    h = (g - b) / delta;
                else if (g == v)
                    h = 2 + (b - r) / delta;
                else if (b == v)
                    h = 4 + (r - g) / delta;

                h *= 60;
                if (h < 0.0)
                    h += 360;

            }

            return new HsvColor(h, s, v / 255);
        }
    }
    #endregion HsvColor

    #region ColorThumb
    public class ColorThumb : System.Windows.Controls.Primitives.Thumb
    {
        static ColorThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorThumb),
                new FrameworkPropertyMetadata(typeof(ColorThumb)));
        }

        public static readonly DependencyProperty ThumbColorProperty =
            DependencyProperty.Register("ThumbColor", typeof(Color), typeof(ColorThumb),
                new FrameworkPropertyMetadata(Colors.Transparent));

        public static readonly DependencyProperty PointerOutlineThicknessProperty =
            DependencyProperty.Register("PointerOutlineThickness", typeof(double), typeof(ColorThumb), 
                new FrameworkPropertyMetadata(1.0));

        public static readonly DependencyProperty PointerOutlineBrushProperty =
            DependencyProperty.Register("PointerOutlineBrush", typeof(Brush), typeof(ColorThumb), 
                new FrameworkPropertyMetadata(null));


        public Color ThumbColor
        {
            get => (Color)GetValue(ThumbColorProperty);
            set => SetValue(ThumbColorProperty, value);
        }

        public double PointerOutlineThickness
        {
            get => (double)GetValue(PointerOutlineThicknessProperty);
            set => SetValue(PointerOutlineThicknessProperty, value);
        }

        public Brush PointerOutlineBrush
        {
            get => (Brush)GetValue(PointerOutlineBrushProperty);
            set => SetValue(PointerOutlineBrushProperty, value);
        }


    }
    #endregion ColorThumb

}