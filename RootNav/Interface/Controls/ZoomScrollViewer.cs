using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RootNav.Interface.Controls
{
    public class ZoomScrollViewer : ScrollViewer
    {
        #region Dependency Variables
        public static readonly DependencyProperty CurrentZoomProperty =
           DependencyProperty.Register("CurrentZoom", typeof(double), typeof(ZoomScrollViewer), new PropertyMetadata(1.0));

        public double CurrentZoom
        {
            get
            {
                return (double)GetValue(CurrentZoomProperty);
            }
            set
            {
                SetValue(CurrentZoomProperty, value);
            }
        }
        #endregion


        #region Variables
        private int ZoomIndex = 6;
        private bool SpecificZoom = false;
        private double[] ZoomFactors = new double[] { 0.2, 0.3, 0.4, 0.5, 0.6, 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0, 3.0, 4.0, 5.0 };
        private Point? lastCenterPositionOnTarget;
        private Point? lastMousePositionOnTarget;
        private Point? lastDragPoint;
        #endregion

        #region Constructors
        static ZoomScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomScrollViewer), new FrameworkPropertyMetadata(typeof(ZoomScrollViewer)));
        }

        public ZoomScrollViewer()
            : base()
        {
        }
        #endregion

        #region Mouse Events
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                this.Cursor = Cursors.SizeAll;
                Mouse.Capture(this);
                Point posNow = e.GetPosition(this);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                this.ScrollToHorizontalOffset(this.HorizontalOffset - dX);
                this.ScrollToVerticalOffset(this.VerticalOffset - dY);
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(this);
            if (mousePos.X <= this.ViewportWidth && mousePos.Y <
                this.ViewportHeight)
            {
                if (this.ScrollableHeight > 0 || this.ScrollableWidth > 0)
                {
                    lastDragPoint = mousePos;
                }
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            System.Collections.IEnumerator IE = LogicalTreeHelper.GetChildren(this).GetEnumerator();
            FrameworkElement child = null;
            if (IE.MoveNext())
            {
                child = IE.Current as FrameworkElement;
                if (child != null)
                {
                    lastMousePositionOnTarget = Mouse.GetPosition(child);
                    this.ChangeZoomIndex(e.Delta);
                    this.ApplyZoom();

                }
            }

            e.Handled = true;
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            System.Console.WriteLine("ZoomArrow: 111");
            this.Cursor = Cursors.Arrow;
            lastDragPoint = null;
            this.ReleaseMouseCapture();
           
        }
        #endregion

        #region Scroll Events
        protected override void OnScrollChanged(ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                System.Collections.IEnumerator IE = LogicalTreeHelper.GetChildren(this).GetEnumerator();
                FrameworkElement child = null;
                if (IE.MoveNext())
                {
                    child = IE.Current as FrameworkElement;
                }
                else
                    return;

                if (child == null)
                    return;


                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(this.ViewportWidth / 2,
                                                         this.ViewportHeight / 2);
                        Point centerOfTargetNow =
                              this.TranslatePoint(centerOfViewport, child);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(child);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / child.ActualWidth;
                    double multiplicatorY = e.ExtentHeight / child.ActualHeight;

                    double newOffsetX = this.HorizontalOffset -
                                        dXInTargetPixels * multiplicatorX;
                    double newOffsetY = this.VerticalOffset -
                                        dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    this.ScrollToHorizontalOffset(newOffsetX);
                    this.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }
        #endregion

        #region Zoom Functions
        public void SelectOptimumZoomLevel(double optimumWidth, double optimumHeight, double desiredWidth, double desiredHeight)
        {
            double widthRatio = optimumWidth / desiredWidth;
            double heightRatio = optimumHeight / desiredHeight;

            // Scale image down
            double scalingFactor = 1.0;
            if (widthRatio < 1.0 && heightRatio < 1.0)
                scalingFactor = Math.Min(widthRatio, heightRatio);
            // Scale image up
            else
                scalingFactor = Math.Min(widthRatio, heightRatio);

            // Apply scaling factor
            this.ApplyZoom(scalingFactor);

            // Update current zoom
            CurrentZoom = scalingFactor;

            // Set most appropriate zoom level
            if (scalingFactor < ZoomFactors.First())
                ZoomIndex = 0;
            else if (scalingFactor > ZoomFactors.Last())
                ZoomIndex = ZoomFactors.Length - 1;
            else
            {
                int i = 0;
                while (ZoomFactors[i] < scalingFactor)
                    i++;
                ZoomIndex = i;
                SpecificZoom = true;
            }
        }

        public void ChangeZoomIndex(int Delta)
        {
            if (Delta > 0)
            {
                if (!SpecificZoom)
                    ZoomIndex = Math.Min((ZoomIndex + 1), ZoomFactors.Length - 1);
            }
            else
            {
                ZoomIndex = Math.Max((ZoomIndex - 1), 0);
            }

            SpecificZoom = false;
        }

        private void ApplyZoom(double scalingFactor)
        {
            System.Collections.IEnumerator IE = LogicalTreeHelper.GetChildren(this).GetEnumerator();
            FrameworkElement child = null;
            if (IE.MoveNext())
            {
                child = IE.Current as FrameworkElement;

                child.LayoutTransform = new ScaleTransform(scalingFactor, scalingFactor);

                var centerOfViewport = new Point(this.ViewportWidth / 2,
                                                 this.ViewportHeight / 2);
                lastCenterPositionOnTarget = this.TranslatePoint(centerOfViewport, child);

                this.CurrentZoom = this.ZoomFactors[this.ZoomIndex];
            }
        }

        private void ApplyZoom()
        {
            ApplyZoom(this.ZoomFactors[this.ZoomIndex]);
            this.CurrentZoom = this.ZoomFactors[this.ZoomIndex];
        }
        #endregion

    }
}
