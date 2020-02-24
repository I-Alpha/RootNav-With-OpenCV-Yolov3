using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;


namespace RootNav.Interface.Controls
{
    public class ControlAdorner : Adorner
    {
        private Grid _child;

        public ControlAdorner(UIElement adornedElement, Point position)
            : base(adornedElement)
        {
            this.Width = 300;
            this.Height = 300;
            this.Margin = new Thickness(position.X - 150, position.Y - 150, 0, 0);
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException();
            return _child;
        }

        public Grid Child
        {
            get { return _child; }
            set
            {
                if (_child != null)
                {
                    RemoveVisualChild(_child);
                }
                _child = value;
                if (_child != null)
                {
                    AddVisualChild(_child);
                }
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(new Point(0, 0), finalSize));
            return new Size(_child.ActualWidth, _child.ActualHeight);
        }
    }
}
