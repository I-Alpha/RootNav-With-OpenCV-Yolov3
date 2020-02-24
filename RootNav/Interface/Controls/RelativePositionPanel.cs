using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RootNav.Interface.Controls
{
    public class RelativePositionPanel : Panel
    {
        public static readonly DependencyProperty RelativePositionXProperty =
            DependencyProperty.RegisterAttached("RelativePositionX", typeof(double), typeof(RelativePositionPanel),
            new FrameworkPropertyMetadata(0.5, new PropertyChangedCallback(RelativePositionPanel.OnRelativePositionChanged)));
        
        public static readonly DependencyProperty RelativePositionYProperty =
            DependencyProperty.RegisterAttached("RelativePositionY", typeof(double), typeof(RelativePositionPanel),
            new FrameworkPropertyMetadata(0.5, new PropertyChangedCallback(RelativePositionPanel.OnRelativePositionChanged)));

        public static double GetRelativePositionX(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (double)element.GetValue(RelativePositionXProperty);
        }

        public static double GetRelativePositionY(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (double)element.GetValue(RelativePositionYProperty);
        }

        public static void SetRelativePositionX(UIElement element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(RelativePositionXProperty, value);
        }

        public static void SetRelativePositionY(UIElement element, double value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(RelativePositionYProperty, value);
        }

        private static void OnRelativePositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement reference = d as UIElement;
            if (reference != null)
            {
                RelativePositionPanel parent = VisualTreeHelper.GetParent(reference) as RelativePositionPanel;
                if (parent != null)
                {
                    parent.InvalidateArrange();
                }
            }
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (UIElement element in base.InternalChildren)
            {
                if (element != null)
                {
                    double x = (arrangeSize.Width - element.DesiredSize.Width) * GetRelativePositionX(element);
                    double y = (arrangeSize.Height - element.DesiredSize.Height) * GetRelativePositionY(element);

                    if (double.IsNaN(x)) x = 0;
                    if (double.IsNaN(y)) y = 0;

                    element.Arrange(new Rect(new Point(x, y), element.DesiredSize));
                }
            }
            return arrangeSize;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = new Size(double.PositiveInfinity, double.PositiveInfinity);

            // SDK docu says about InternalChildren Property: 'Classes that are derived from Panel 
            // should use this property, instead of the Children property, for internal overrides 
            // such as MeasureCore and ArrangeCore.

            foreach (UIElement element in this.InternalChildren)
            {
                if (element != null)
                    element.Measure(size);
            }

            return base.MeasureOverride(availableSize);
        }
    }
}
