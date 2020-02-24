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
using RootNav.Core.MixtureModels;

namespace RootNav.Interface.Controls
{
    public class GaussianMixtureModelViewer : ContentControl
    {
        public delegate void ThresholdChangedHandler(int index);
        public event ThresholdChangedHandler ThresholdChanged;
        
        private static Thickness GraphPadding = new Thickness(16, 10, 10, 20);
        private static double tickLength = 4;
        private static double fontSize = 10.0;
        private static double fontPadding = 6.0;

        private double indexLocation = -1;
        private int highlightedIndex = -1;

        private bool weightedComponents = true;
        public bool WeightedComponents
        {
            get { return weightedComponents; }
            set { weightedComponents = value; }
        }

        private bool showMixtureDistribution = false;
        public bool ShowMixtureDistribution
        {
            get { return showMixtureDistribution; }
            set { showMixtureDistribution = value; }
        }

        private bool scaleXAxis = false;

        public bool ScaleXAxis
        {
            get { return scaleXAxis; }
            set { scaleXAxis = value; }
        }

        private bool cumulative = false;

        public bool Normalised
        {
            get { return cumulative; }
            set { cumulative = value; }
        }

        public EMConfiguration Configuration { get; set; }

        static GaussianMixtureModelViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GaussianMixtureModelViewer),
                new FrameworkPropertyMetadata(typeof(GaussianMixtureModelViewer)));
        }

        public GaussianMixtureModelViewer()
        {
            this.MouseMove += new MouseEventHandler(GaussianMixtureModelViewer_MouseMove);
            this.MouseLeave += new MouseEventHandler(GaussianMixtureModelViewer_MouseLeave);
            this.MouseRightButtonDown += new MouseButtonEventHandler(GaussianMixtureModelViewer_MouseRightButtonDown);
        }

        void GaussianMixtureModelViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.indexLocation >= 0)
            {
                this.ContextMenu = new ContextMenu();
                MenuItem mi = new MenuItem() { Header = "Threshold here" };
                mi.Click += new RoutedEventHandler(ThresholdHereClick);
                this.ContextMenu.Items.Add(mi);
            }
        }


        void ThresholdHereClick(object sender, RoutedEventArgs e)
        {
            if (this.ThresholdChanged != null)
            {
                this.ThresholdChanged(this.highlightedIndex);
            }
        }

        void GaussianMixtureModelViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            this.indexLocation = -1;
        }

        void GaussianMixtureModelViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.ContextMenu == null || this.ContextMenu.IsVisible != true)
            {
                double left = GraphPadding.Left + 0.5,
                         right = ActualWidth - GraphPadding.Right + 0.5,
                         top = GraphPadding.Top + 0.5,
                         bottom = ActualHeight - GraphPadding.Bottom + 0.5;

                Point mousePosition = e.GetPosition(this);
                if (mousePosition.X > left && mousePosition.X < right && mousePosition.Y > top && mousePosition.Y < bottom)
                {
                    this.indexLocation = (mousePosition.X - left) / (right - left);
                    this.InvalidateVisual();
                }
                else
                {
                    if (this.indexLocation != -1)
                    {
                        this.indexLocation = -1;
                        this.InvalidateVisual();
                    }

                }
            }
        }

        private int[] histogramData = null;

        public int[] HistogramData
        {
            get { return histogramData; }
        }

        private double[] normalisedData = null;
        private double[] stretchedData = null;

        private GaussianMixtureModel mixtureModel = null;

        public GaussianMixtureModel MixtureModel
        {
            get { return mixtureModel; }
        }

        public void SetData(int[] histogramData, GaussianMixtureModel gmm, EMConfiguration config)
        {
            this.Configuration = config;
            this.histogramData = histogramData;
            this.mixtureModel = gmm;

            double total = this.histogramData.Sum();
            int length = this.histogramData.Length;
            int max = this.histogramData.Max();
            this.normalisedData = new double[length];
            this.stretchedData = new double[length];

            for (int h = 0; h < length; h++)
            {
                this.normalisedData[h] = this.histogramData[h] / total;
                this.stretchedData[h] = this.histogramData[h] / (double)max;
            }
            this.InvalidateVisual();

        }

        public void ReCalculateData()
        {
            double total = this.histogramData.Sum();
            int length = this.histogramData.Length;
            int max = this.histogramData.Max();
            this.normalisedData = new double[length];
            this.stretchedData = new double[length];

            for (int h = 0; h < length; h++)
            {
                this.normalisedData[h] = this.histogramData[h] / total;
                this.stretchedData[h] = this.histogramData[h] / (double)max;
            }
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {  
            if (this.IsInitialized == false || this.Height <= 0)
                return;

            this.SnapsToDevicePixels = true; 
            // Background

            Rect background = new Rect(0, 0, this.ActualWidth, this.ActualHeight);

            // Transparent hit rectangle

            if (this.Background == null)
            {
                drawingContext.DrawRectangle(Brushes.Transparent, null, background);
            }
            else
            {
                drawingContext.DrawRoundedRectangle(this.Background, null, background, 4.0, 4.0);
            }

            // Push border clipping region
            drawingContext.PushClip(new RectangleGeometry(background, 4.0,4.0));

            // Dimensions of graphing region
            double left = GraphPadding.Left + 0.5,
                   right = ActualWidth - GraphPadding.Right + 0.5,
                   top = GraphPadding.Top + 0.5,
                   bottom = ActualHeight - GraphPadding.Bottom + 0.5;

            // Initialise data bounds
            int leftDataBound = 0;
            int rightDataBound = 255;

            // Draw histogram data
            drawingContext.PushClip(new RectangleGeometry(new Rect(left + 1, top - 1, right - left - 1, bottom - top + 1), 0.0, 0.0));
            top += 6;
            if (this.histogramData != null)
            {
                // Find true data bounds
                rightDataBound = histogramData.Length;
                if (this.ScaleXAxis)
                {
                    for (int i = 0; i < this.histogramData.Length; i++)
                    {
                        if (this.histogramData[i] > 0)
                        {
                            leftDataBound = i;
                            break;
                        }

                    }

                    for (int i = this.histogramData.Length - 1; i >= 0; i--)
                    {
                        if (this.histogramData[i] > 0)
                        {
                            rightDataBound = i;
                            break;
                        }

                    }

                    if (this.mixtureModel != null && this.mixtureModel.K != this.mixtureModel.ThresholdK)
                    {
                        int rightDataBoundGMM = 0;
                        int k = this.MixtureModel.K - 1;
                        double max = this.mixtureModel.SingleGaussianProbability(this.mixtureModel.Mean[k], k);

                        for (int i = this.histogramData.Length - 1; i >= 0; i--)
                        {
                            double p = (this.mixtureModel.SingleGaussianProbability(i, k) / max);

                            if (this.weightedComponents)
                                p *= this.mixtureModel.MixingWeight[k];

                            if (p > 0.001)
                            {
                                rightDataBoundGMM = i;
                                break;
                            }
                        }

                        rightDataBound = Math.Max(rightDataBound, rightDataBoundGMM);
                    }
                }

                StreamGeometry histogramGeometry = new StreamGeometry();
                using (StreamGeometryContext sgc = histogramGeometry.Open())
                {
                    int length = this.histogramData.Length;

                    double currentOffset = left;
                    double columnWidth = (right - left) / (rightDataBound - leftDataBound);

                    for (int i = leftDataBound; i < rightDataBound; i++)
                    {
                        if (this.stretchedData[i] == 0)
                        {
                            currentOffset += columnWidth;
                            continue;
                        }
                        double columnTop = bottom - ((bottom - top) * this.stretchedData[i]);

                        sgc.BeginFigure(new Point(currentOffset + 0.5,columnTop + 0.5), true, true);
                        sgc.LineTo(new Point(currentOffset + columnWidth + 0.5, columnTop + 0.5), false, false);
                        sgc.LineTo(new Point(currentOffset + columnWidth + 0.5, bottom + 0.5), false, false);
                        sgc.LineTo(new Point(currentOffset + 0.5, bottom + 0.5), false, false);

                        currentOffset += columnWidth;
                    }
                }

                histogramGeometry.Freeze();

                drawingContext.DrawGeometry(Brushes.LightGray, null, histogramGeometry);
            
                if (this.mixtureModel != null && this.mixtureModel.K != this.mixtureModel.ThresholdK)
                {
                    int histogramLength = this.histogramData.Length;
                    for (int k = 0; k <= this.mixtureModel.K; k++)
                    {
                        Pen mixturePen = GetMixturePen(k, this.mixtureModel.K, this.mixtureModel.ThresholdK);

                        StreamGeometry distribution = new StreamGeometry();
                        using (StreamGeometryContext sgc = distribution.Open())
                        {
                            if (k < this.mixtureModel.K)
                            {
                                if (this.Normalised)
                                {
                                    double currentOffset = left;
                                    double columnWidth = (right - left) / (rightDataBound - leftDataBound);
                                    for (int i = leftDataBound; i < rightDataBound; i++)
                                    {

                                        double p = this.mixtureModel.NormalisedProbability(i, k);

                                        if (i == leftDataBound)
                                            sgc.BeginFigure(new Point(currentOffset + 0.5, bottom - ((bottom - top) * p) + 0.5), true, false);
                                        else
                                            sgc.LineTo(new Point(currentOffset + 0.5, bottom - ((bottom - top) * p) + 0.5), true, false);

                                        currentOffset += columnWidth;

                                    }
                                }
                                else
                                {
                                    double max = this.mixtureModel.SingleGaussianProbability(this.mixtureModel.Mean[k], k);

                                    double currentOffset = left;
                                    double columnWidth = (right - left) / (rightDataBound - leftDataBound);
                                    for (int i = leftDataBound; i < rightDataBound; i++)
                                    {
                                        double p = (this.mixtureModel.SingleGaussianProbability(i, k) / max);

                                        if (this.weightedComponents)
                                            p *= this.mixtureModel.MixingWeight[k];

                                        if (i == leftDataBound)
                                            sgc.BeginFigure(new Point(currentOffset + 0.5, bottom - ((bottom - top) * p) + 0.5), true, false);
                                        else
                                            sgc.LineTo(new Point(currentOffset + 0.5, bottom - ((bottom - top) * p) + 0.5), true, false);

                                        currentOffset += columnWidth;
                                    }
                                }
                            }
                            else if (k == this.mixtureModel.K && this.ShowMixtureDistribution)
                            {

                                int maxk = 0;
                                double max = this.mixtureModel.MixtureMaximum(out maxk);

                                double currentOffset = left;
                                double columnWidth = (right - left) / (rightDataBound - leftDataBound);
                                for (int i = leftDataBound; i < rightDataBound; i++)
                                {
                                    double p = (this.mixtureModel.MixingProbability(i) / max);

                                    if (this.weightedComponents)
                                        p *= this.mixtureModel.MixingWeight[maxk];

                                    if (i == leftDataBound)
                                        sgc.BeginFigure(new Point(currentOffset + 0.5, bottom - ((bottom - top) * p) + 0.5), true, false);
                                    else
                                        sgc.LineTo(new Point(currentOffset + 0.5, bottom - ((bottom - top) * p) + 0.5), true, false);

                                    currentOffset += columnWidth;
                                }
                         
                            }
                        }
                        distribution.Freeze();
                        drawingContext.DrawGeometry(null, mixturePen, distribution);
                    }
                }
            }
            
            // Pop content clipping region
            drawingContext.Pop();

            // Draw axis
            top -= 6;
            StreamGeometry axisGeometry = new StreamGeometry();
            using (StreamGeometryContext sgc = axisGeometry.Open())
            {
                // Arrowhead
                sgc.BeginFigure(new Point(left - tickLength, top + tickLength), true, false);
                sgc.LineTo(new Point(left, top), true, true);
                sgc.LineTo(new Point(left + tickLength, top + tickLength), true, false);

                // Main Vertical Axis               
                sgc.BeginFigure(new Point(left, top), false, false);
                sgc.LineTo(new Point(left, bottom), true, false);
                sgc.LineTo(new Point(left - tickLength, bottom), true, false);

                // Main Horizontal Axis
                sgc.BeginFigure(new Point(left, bottom + tickLength), false, false);
                sgc.LineTo(new Point(left, bottom), true, false);
                sgc.LineTo(new Point(right, bottom), true, false);
                sgc.LineTo(new Point(right, bottom + tickLength), true, false);

                // Index drawing
                if (this.indexLocation >= 0)
                {
                    double xposition = (int)(this.indexLocation * (right - left) + left) + 0.5;
                    sgc.BeginFigure(new Point(xposition, top + 5.0), false, false);
                    sgc.LineTo(new Point(xposition, bottom), true, false);
                }
            }

            SolidColorBrush uiColor = Brushes.DarkGray;

            Pen axisPen = new Pen(uiColor, 1.0);
            axisGeometry.Freeze();
            drawingContext.DrawGeometry(uiColor, axisPen, axisGeometry);

            // Axis numbering - Vertical
            FormattedText verticalNumbering = new FormattedText("0", System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), fontSize, uiColor);
            drawingContext.DrawText(verticalNumbering, new Point(left - verticalNumbering.Width - fontPadding, bottom - (verticalNumbering.Height / 2)));
            verticalNumbering = new FormattedText("p", System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), fontSize, uiColor);
            drawingContext.DrawText(verticalNumbering, new Point(left - verticalNumbering.Width - fontPadding, top - (verticalNumbering.Height / 3)));

            // Axis numbering - Horizontal
            verticalNumbering = new FormattedText(leftDataBound.ToString(), System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), fontSize, uiColor);
            drawingContext.DrawText(verticalNumbering, new Point(left - verticalNumbering.Width / 2, bottom + fontPadding));
            verticalNumbering = new FormattedText(rightDataBound.ToString(), System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), fontSize, uiColor);
            drawingContext.DrawText(verticalNumbering, new Point(right - verticalNumbering.Width / 2, bottom + fontPadding));

            // Index drawing
            if (this.indexLocation >= 0)
            {
                this.highlightedIndex = (int)((rightDataBound - leftDataBound) * this.indexLocation + leftDataBound);
                double xposition = (int)(this.indexLocation * (right - left) + left) + 0.5;
                verticalNumbering = new FormattedText(highlightedIndex.ToString(), System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), fontSize, uiColor);
                if (this.indexLocation > 0.9)
                {
                    drawingContext.DrawText(verticalNumbering, new Point(xposition - 5 - verticalNumbering.Width, top + 10));
                }
                else
                {
                    drawingContext.DrawText(verticalNumbering, new Point(xposition + 5, top + 10));
                }
            }

            // Pop border clipping region
            drawingContext.Pop();

            base.OnRender(drawingContext);
        }

        private Pen GetMixturePen(int k, int K, int ThresholdK)
        {
            double PenWidth = 2.0;
            if (k == K)
            {
                Pen combinedPen = new Pen(Brushes.Black, PenWidth);
                combinedPen.DashStyle = DashStyles.Dot;
                return combinedPen;
            }

            if (ThresholdK == K - 1 || k <= ThresholdK) return new Pen(Brushes.DarkGray, PenWidth);

            if (k == K - 1)
            {
                // Blue Pen
                return new Pen(new SolidColorBrush(Color.FromRgb(50, 50, 255)), PenWidth);
            }
            else
            {
                return new Pen(new SolidColorBrush(Color.FromRgb(50, 255, 50)), PenWidth);
            }
        }
    }
}
