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
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Media.Effects;

using RootNav.Interface.Windows;
using RootNav.Core.LiveWires;
using RootNav.Core.Measurement;
using RootNav.Measurement;
using RootNav.Data;

namespace RootNav.Interface.Controls
{
    public class RootDetectionScreenOverlay : Adorner
    {
        #region Static Fields
        private const double TerminalSize = 10.0;
        private const double TerminalUISize = 11.0;
        private const double SnapToTipThreshold = 60.0;
        private const double ControlPointUISize = 7.0;
        private const double RootUISize = 10;
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty DetectionModeProperty =
            DependencyProperty.Register("DetectionMode", typeof(DetectionToolbox.RootTerminalControlMode), typeof(RootDetectionScreenOverlay), new PropertyMetadata(DetectionToolbox.RootTerminalControlMode.None));

        public DetectionToolbox.RootTerminalControlMode DetectionMode
        {
            get
            {
                return (DetectionToolbox.RootTerminalControlMode)GetValue(DetectionModeProperty);
            }
            set
            {
                SetValue(DetectionModeProperty, value);
            }
        }

        public static readonly DependencyProperty CurrentZoomProperty =
         DependencyProperty.Register("CurrentZoom", typeof(double), typeof(RootDetectionScreenOverlay), new PropertyMetadata(1.0));

        private Cursor grabbing = null;

        public Cursor Grabbing
        {
            get {
                if (grabbing == null)
                {
                    Uri uri = new Uri("Themes/grabbing.cur", UriKind.Relative);
                    this.grabbing = new Cursor(App.GetResourceStream(uri).Stream);
                }
                return grabbing;
            }
        }

        public static readonly DependencyProperty IsBusyProperty =
         DependencyProperty.Register("IsBusy", typeof(bool), typeof(RootDetectionScreenOverlay), new PropertyMetadata(false));
        
        public bool IsBusy
        {
            get
            {
                return (bool)GetValue(IsBusyProperty);
            }
            set
            {
                SetValue(IsBusyProperty, value);
            }
        }

        private ControlPointDragInfo controlPointDragInfo = null;
        private TerminalDragInfo terminalDragInfo = null;
        private RootDragInfo rootDragInfo = null;

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

        public double CurrentUIScale
        {
            get
            {
                return Math.Min(4.0, Math.Max(0.6, 1.0 / this.CurrentZoom));
            }
        }
        #endregion

        #region Properties
        public OverlayStage Stage { get; set; }

        private ScreenOverlayRenderInfo renderInfo = new ScreenOverlayRenderInfo();

        private RootTerminalCollection terminalCollection = new RootTerminalCollection();
        private RootInfo HighlightedSplineRootInfo { get; set; }
        private List<ControlAdorner> terminalRootAdorners = new List<ControlAdorner>();
        private List<TerminalRootSelector> terminalRootSelectors = new List<TerminalRootSelector>();

        public RootTerminalCollection Terminals
        {
            get { return terminalCollection; }
        }

        private bool snapToTip = false;

        public bool SnapToTip
        {
            get
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    return false;
                }

                return snapToTip;
            }
            set
            {
                if (!value)
                {
                    RemoveTipHighlight();
                }
                snapToTip = value;
            }
        }

        private Point mousePosition = default(Point);
        private bool IsDragInProgress
        {
            get
            {
                return isControlPointDragInProgress || isTerminalPointDragInProgress || isRootDragInProgress;
            }
        }
        private bool isControlPointDragInProgress = false;
        private bool isTerminalPointDragInProgress = false;
        private bool isRootDragInProgress = false;

        private LiveWirePathCollection paths = new LiveWirePathCollection();

        public LiveWirePathCollection Paths
        {
            get { return paths; }
            set { paths = value; }
        }

        private RootBase currentHighlightedRoot = null;

        public RootBase CurrentHighlightedRoot
        {
            get { return currentHighlightedRoot; }
            set { currentHighlightedRoot = value; }
        }

        private List<List<Point>> sampledRoots = new List<List<Point>>();

        private RootCollection roots = new RootCollection();

        public RootCollection Roots
        {
            get { return roots; }
            set { roots = value; }
        }

        RootInfo.DragType? measurementAnglePointType = null;
        double measurementAngleDualDragMinimumOffset = 0;
        double measurementAngleDualDragMaximumOffset = 0;
        bool isMeasurementAngleDragInProgress = false;

        private ControlAdorner tipHighlightAdorner = null;
        private Point tipHighlightAdornerPosition;

        private int currentHighlightedRootIndex = -1;

        public int CurrentHighlightedRootIndex
        {
            get { return currentHighlightedRootIndex; }
            set { currentHighlightedRootIndex = value; }
        }
        private TerminalType currentDragTerminalType = TerminalType.Undefined;
        private TerminalType currentHighlightedTerminalType = TerminalType.Undefined;
        private int currentHighlightedControlPointIndex = -1;
        private int currentHighlightedTerminalIndex = -1;

        private const int SampleRate = 1;

        private SplinePositionReference rootPositionReference = null;
        private Point rootPoint = default(Point);

        public Point RootPoint
        {
            get { return rootPoint; }
            set { rootPoint = value; }
        }

        public void ClearAll()
        {
            this.paths.Clear();
            this.sampledRoots.Clear();
        }

        public void ClearLaterals()
        {
            this.paths.ClearLaterals();
            this.RecalculateAllSamples();
        }

        public void ResetAll()
        {
            this.ResetCalled = true;

            this.rootPoint = default(Point);

            this.Stage = OverlayStage.Detection;
            this.terminalCollection = new RootTerminalCollection();
            this.HighlightedSplineRootInfo = null;
            
            this.terminalRootAdorners.Clear();
            this.terminalRootSelectors.Clear();
            this.mousePosition = default(Point);
            this.isControlPointDragInProgress = false;
            this.isTerminalPointDragInProgress = false;
            this.isRootDragInProgress = false;
            this.paths.Clear();
            this.currentHighlightedRoot = null;
            this.sampledRoots.Clear();
            this.roots = new RootCollection();
            this.isMeasurementAngleDragInProgress = false;
            this.measurementAnglePointType = null;
            this.currentHighlightedRootIndex = -1;
            this.currentHighlightedControlPointIndex = -1;
            this.currentHighlightedTerminalIndex = -1;
        }

        void PathAdded()
        {
            LiveWirePath path = this.paths.Last();

            List<Point> sampled = new List<Point>();
            for (int i = 0; i < path.Path.Count; i += SampleRate)
            {
                sampled.Add(path.Path[i]);
            }
            sampledRoots.Add(sampled);

            this.renderInfo.RootCount = sampledRoots.Count; 
            this.InvalidateVisual();
        }

        public void RecalculateSamples(int index)
        {
            LiveWirePath path = this.paths[index];

            List<Point> newPath = new List<Point>();
            for (int i = 0; i < path.Path.Count; i += SampleRate)
            {
                newPath.Add(path.Path[i]);
            }

            sampledRoots[index] = newPath;
            this.InvalidateVisual();
        }

        public void InitialiseMeasurementStage(int splineResolution, double unitConversion)
        {
            this.Roots = RootBase.CreateRootSystem(this.paths, this.terminalCollection, renderInfo.HighlightedRootColors, splineResolution, unitConversion);

            this.Stage = OverlayStage.Measurement;
            this.InvalidateVisual();
        }

        public void BackFromMeasurementStage()
        {
            this.Roots = null;
            this.Stage = OverlayStage.Detection;
            this.InvalidateVisual();
        }

        public void RecalculateAllSamples()
        {
            this.sampledRoots.Clear();
            for (int index = 0; index < this.paths.Count; index++)
            {
                LiveWirePath path = this.paths[index];
                List<Point> newPath = new List<Point>();
                for (int i = 0; i < path.Path.Count; i += SampleRate)
                {
                    newPath.Add(path.Path[i]);
                }
                sampledRoots.Add(newPath);
            }
            
            this.InvalidateVisual();
        }

        private int patchSize = 0;

        public int PatchSize
        {
            get { return patchSize; }
            set { patchSize = value; }
        }

        private List<Point> tipAnchorPoints = new List<Point>();

        public List<Point> TipAnchorPoints
        {
            get { return tipAnchorPoints; }
            set { tipAnchorPoints = value; }
        }

        #endregion

        private bool shiftAdd = false;

        public bool LinkAdd
        {
            get { return shiftAdd; }
            set { shiftAdd = value; }
        }

        public RootDetectionScreenOverlay(UIElement adornedElement, DetectionToolbox toolboxController, ZoomScrollViewer scrollViewer) :
            base(adornedElement)
        {
            this.paths.PathAdded += new LiveWirePathCollection.PathAddedEventHandler(PathAdded);

            Binding binding = new Binding("Mode");
            binding.Source = toolboxController;
            this.SetBinding(RootDetectionScreenOverlay.DetectionModeProperty, binding);

            binding = new Binding("CurrentZoom");
            binding.Source = scrollViewer;
            this.SetBinding(RootDetectionScreenOverlay.CurrentZoomProperty, binding);

            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromName("CurrentZoom", typeof(RootDetectionScreenOverlay), typeof(RootDetectionScreenOverlay));
            descriptor.AddValueChanged(this, delegate(object sender, EventArgs args) { if (this.Visibility == System.Windows.Visibility.Visible) this.InvalidateVisual(); });
        }

        #region Rendering
        private void RenderLinks()
        {
            foreach (var pair in this.terminalCollection.TerminalLinks)
            {
                currentRenderDrawingContext.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1.0 * CurrentUIScale), pair.Item1.Position, pair.Item2.Position);
            }
        }

        private void RenderRoots()
        {
            if (this.paths.Count != this.sampledRoots.Count)
            {
                return;
            }

            for (int i = 0; i < this.sampledRoots.Count; i++)
            {
                if (this.currentHighlightedRootIndex == i)
                    continue;

                Pen rootPen = renderInfo.RootPens[i];

                // Render lateral path in source primary colour
                if (this.paths[i] is LiveWireLateralPath)
                {
                    rootPen = renderInfo.RootPens[(this.paths[i] as LiveWireLateralPath).TargetPoint.ParentIndex];
                }

                List<Point> points = this.sampledRoots[i];

                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext sgc = streamGeometry.Open())
                {
                    int count = points.Count;
                    sgc.BeginFigure(points[0], false, false);
                    for (int j = 0; j < count; j++)
                    {
                        sgc.LineTo(points[j], true, false);
                    }
                }
                if (streamGeometry.CanFreeze)
                    streamGeometry.Freeze();

                currentRenderDrawingContext.DrawGeometry(null, rootPen, streamGeometry);

            }

        }

        private void RenderSplineRoot(RootBase r)
        {
            // Selected ui components
            if (r.IsSelected)
            {
                // Show convex hull
                if (r.ConvexHullPoints != null && r.Order < 0)
                {
                    StreamGeometry hullGeometry = new StreamGeometry();
                    using (StreamGeometryContext sgc = hullGeometry.Open())
                    {
                        int count = r.ConvexHullPoints.Count;
                        sgc.BeginFigure((Point)r.ConvexHullPoints[0], true, true);
                        for (int j = 1; j < count; j++)
                        {
                            sgc.LineTo((Point)r.ConvexHullPoints[j], true, false);
                        }
                    }

                    currentRenderDrawingContext.DrawGeometry(renderInfo.ConvexHullFillBrush, renderInfo.ConvexHullPen, hullGeometry);
                }
            }

            if (r.Order >= 0)
            {
                Pen rootPen = renderInfo.RootPens[0];
                if (r.PrimaryParent == null || r.PrimaryParent.Order == -1)
                {
                    rootPen = renderInfo.RootPens[r.RootIndex];

                    if (r.IsHighlighted || r.IsSelected)
                        rootPen = renderInfo.HighlightedRootPens[r.RootIndex];
                }
                else
                {
                    rootPen = renderInfo.RootPens[r.PrimaryParent.RootIndex];

                    if (r.IsHighlighted || r.IsSelected)
                        rootPen = renderInfo.HighlightedRootPens[r.PrimaryParent.RootIndex];
                }

                Point[] points = r.Spline.SampledPoints;

                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext sgc = streamGeometry.Open())
                {
                    int count = points.Length;
                    sgc.BeginFigure(points[0], false, false);
                    for (int j = 1; j < count; j++)
                    {
                        sgc.LineTo(points[j], true, false);
                    }
                }
                if (streamGeometry.CanFreeze)
                    streamGeometry.Freeze();

                currentRenderDrawingContext.DrawGeometry(null, rootPen, streamGeometry);

            }

            foreach (RootBase child in r.Children)
            {
                RenderSplineRoot(child);
            }

           
        }

        private void RenderSplineRootInfo()
        {
            if (this.HighlightedSplineRootInfo != null)
            {
                this.HighlightedSplineRootInfo.RenderRootInfo(this.currentRenderDrawingContext, CurrentUIScale);
            }
        }

        private void RenderSplineRoots()
        {
            if (this.roots == null || this.roots.RootTree.Count == 0)
            {
                return;
            }

            foreach(RootBase r in this.roots)
            {
                RenderSplineRoot(r);
            }
        }

        private void RenderHighlightedRoot()
        {
            if (this.paths.Count != this.sampledRoots.Count)
            {
                return;
            }

            if (this.currentHighlightedRootIndex >= 0 && this.currentHighlightedRootIndex < this.sampledRoots.Count)
            {
                Pen highlightRootPen = renderInfo.HighlightedRootPens[this.currentHighlightedRootIndex];

                // Render lateral path in source primary colour
                if (this.paths[this.currentHighlightedRootIndex] is LiveWireLateralPath)
                {
                    highlightRootPen = renderInfo.HighlightedRootPens[(this.paths[this.currentHighlightedRootIndex] as LiveWireLateralPath).TargetPoint.ParentIndex];
                }

                List<Point> points = this.sampledRoots[this.currentHighlightedRootIndex];

                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext sgc = streamGeometry.Open())
                {
                    int count = points.Count;
                    sgc.BeginFigure(points[0], false, false);
                    for (int j = 0; j < count; j++)
                    {
                        sgc.LineTo(points[j], true, false);
                    }
                }
                if (streamGeometry.CanFreeze)
                    streamGeometry.Freeze();

                currentRenderDrawingContext.DrawGeometry(null, highlightRootPen, streamGeometry);
            }
        }

        private void RenderControlPoints()
        {
            if (this.paths != null && this.paths.Count > 0)
            {
                for (int i = 0; i < this.paths.Count; i++)
                {
                    LiveWirePath currentRoot = this.paths[i];
                    Pen strokePen = new Pen(new SolidColorBrush(renderInfo.HighlightedRootColors[i]), 1.0 * CurrentUIScale);

                    if (currentRoot is LiveWireLateralPath)
                    {
                        strokePen = new Pen(new SolidColorBrush(renderInfo.HighlightedRootColors[(currentRoot as LiveWireLateralPath).TargetPoint.ParentIndex]), 1.0 * CurrentUIScale);
                    }

                    for (int index = 0; index < currentRoot.Indices.Count; index++)
                    {
                        int pathIndex = currentRoot.Indices[index];

                        if (i != this.currentHighlightedRootIndex || index != this.currentHighlightedControlPointIndex)
                        {
                            Point p = currentRoot.Path[pathIndex];
                            currentRenderDrawingContext.DrawEllipse(Brushes.LightGray, strokePen, p, 2.5 * CurrentUIScale, 2.5 * CurrentUIScale);
                        }
                    }
                }
            }
        }

        private void RenderTerminalPoints()
        {
            for (int currentIndex = 0; currentIndex < this.terminalCollection.Count; currentIndex++)
            {
                RootTerminal terminal = this.terminalCollection[currentIndex];

                if (this.currentHighlightedTerminalIndex == currentIndex)
                {
                    if (this.isTerminalPointDragInProgress)
                    {
                        continue;
                    } 

                    currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalHighlightEllipseBrush, null, terminal.Position, 14 * CurrentUIScale, 14 * CurrentUIScale);
                }

                if (terminal.Type == TerminalType.Primary)
                {
                    currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalPrimaryStrokePen, terminal.Position, TerminalSize * CurrentUIScale, TerminalSize * CurrentUIScale);
                    currentRenderDrawingContext.DrawText(renderInfo.TerminalPrimaryText, new Point(terminal.Position.X, terminal.Position.Y - (renderInfo.TerminalPrimaryText.Height / 2.0)));
                }
                else if (terminal.Type == TerminalType.Source)
                {
                    currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalSourceStrokePen, terminal.Position, TerminalSize * CurrentUIScale, TerminalSize * CurrentUIScale);
                    currentRenderDrawingContext.DrawText(renderInfo.TerminalSourceText, new Point(terminal.Position.X, terminal.Position.Y - (renderInfo.TerminalSourceText.Height / 2.0)));
                }
                else if (terminal.Type == TerminalType.Lateral)
                { 
                    currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalLateralStrokePen, terminal.Position, TerminalSize * CurrentUIScale, TerminalSize * CurrentUIScale);
                    currentRenderDrawingContext.DrawText(renderInfo.TerminalLateralText, new Point(terminal.Position.X, terminal.Position.Y - (renderInfo.TerminalLateralText.Height / 2.0)));
                }
                else if (terminal.Type == TerminalType.Undefined)
                {
                    currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalUndefinedStrokePen, terminal.Position, TerminalSize * CurrentUIScale, TerminalSize * CurrentUIScale);
                }
            }

            if (this.isTerminalPointDragInProgress && this.mousePosition != default(Point))
            {
                switch (this.currentHighlightedTerminalType)
                {
                    case TerminalType.Primary:
                        currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalPrimaryStrokePen, mousePosition, 8 * CurrentUIScale, 8 * CurrentUIScale);
                        currentRenderDrawingContext.DrawText(renderInfo.TerminalPrimaryText, new Point(mousePosition.X, mousePosition.Y - (renderInfo.TerminalPrimaryText.Height / 2.0)));
                        break;
                    case TerminalType.Source:
                        currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalSourceStrokePen, mousePosition, 8 * CurrentUIScale, 8 * CurrentUIScale);
                        currentRenderDrawingContext.DrawText(renderInfo.TerminalSourceText, new Point(mousePosition.X, mousePosition.Y - (renderInfo.TerminalSourceText.Height / 2.0)));
                        break;
                    case TerminalType.Lateral:
                        currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalLateralStrokePen, mousePosition, 8 * CurrentUIScale, 8 * CurrentUIScale);
                        currentRenderDrawingContext.DrawText(renderInfo.TerminalLateralText, new Point(mousePosition.X, mousePosition.Y - (renderInfo.TerminalLateralText.Height / 2.0)));
                        break;
                    case TerminalType.Undefined:
                        currentRenderDrawingContext.DrawEllipse(renderInfo.TerminalBackgroundBrush, renderInfo.TerminalUndefinedStrokePen, mousePosition, 8 * CurrentUIScale, 8 * CurrentUIScale);
                        break; 
                }
            }
        }

        private void RenderHighlightedControlPoint()
        {
            if (this.currentHighlightedRootIndex >= 0 && this.currentHighlightedControlPointIndex >= 0 )
            {
                if (this.currentHighlightedControlPointIndex < this.paths[this.currentHighlightedRootIndex].Indices.Count)
                {
                    LiveWirePath currentRoot = this.paths[this.currentHighlightedRootIndex];

                    int pathIndex = currentRoot.Indices[this.currentHighlightedControlPointIndex];

                    Pen strokePen = new Pen(new SolidColorBrush(renderInfo.HighlightedRootColors[this.currentHighlightedRootIndex]), 1.0 * CurrentUIScale);

                    if (currentRoot is LiveWireLateralPath)
                    {
                        strokePen = new Pen(new SolidColorBrush(renderInfo.HighlightedRootColors[(currentRoot as LiveWireLateralPath).TargetPoint.ParentIndex]), 1.0 * CurrentUIScale);
                    }

                    Point p = currentRoot.Path[pathIndex];
                    currentRenderDrawingContext.DrawEllipse(renderInfo.ControlPointHighlightEllipseBrush, null, p, 5.0 * CurrentUIScale, 5.0 * CurrentUIScale);
                    currentRenderDrawingContext.DrawEllipse(Brushes.LightGray, strokePen, p, 2.5 * CurrentUIScale, 2.5 * CurrentUIScale);
                }
            }
        }

        private void RenderBackground()
        {
            currentRenderDrawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(0, 0), DesiredSize));
        }

        private void RenderControlPointDragInfo()
        {
            Pen strokePen = new Pen(new SolidColorBrush(renderInfo.HighlightedRootColors[this.controlPointDragInfo.ColorIndex]), 1.0 * CurrentUIScale);
            currentRenderDrawingContext.DrawLine(renderInfo.ControlPointDragLineStrokePen, this.controlPointDragInfo.StartPoint, mousePosition);
            currentRenderDrawingContext.DrawLine(renderInfo.ControlPointDragLineStrokePen, mousePosition, this.controlPointDragInfo.EndPoint);
            currentRenderDrawingContext.DrawEllipse(Brushes.White, strokePen, this.mousePosition, 4 * CurrentUIScale, 4 * CurrentUIScale);
        }

        private void RenderRootDragInfo()
        {
            Brush fillBrush = new SolidColorBrush(renderInfo.HighlightedRootColors[this.rootDragInfo.ColorIndex]);
            currentRenderDrawingContext.DrawLine(renderInfo.ControlPointDragLineStrokePen, this.rootDragInfo.StartPoint, mousePosition);
            currentRenderDrawingContext.DrawEllipse(fillBrush, null, this.mousePosition, 4 * CurrentUIScale, 4 * CurrentUIScale);
        }

        private void RenderTerminalDragInfo()
        {
            if (this.terminalDragInfo != null)
            {
                for (int index = 0; index < this.terminalDragInfo.StartPoints.Count; index++)
                {
                    currentRenderDrawingContext.DrawLine(renderInfo.ControlPointDragLineStrokePen, this.terminalDragInfo.StartPoints[index], this.mousePosition);
                }
            }
        }

        private void RenderGrid()
        {
            if (this.patchSize > 0)
            {
                for (int x = patchSize; x < this.ActualWidth; x += patchSize)
                {
                    currentRenderDrawingContext.DrawLine(renderInfo.GridLinePen, new Point(x, 0), new Point(x, this.ActualHeight));
                }

                for (int y = patchSize; y < this.ActualHeight; y += patchSize)
                {
                    currentRenderDrawingContext.DrawLine(renderInfo.GridLinePen, new Point(0, y), new Point(this.ActualWidth, y));
                }
            }
        }

        private void RenderHighlightedRootPoint()
        {
            if (this.paths[this.currentHighlightedRootIndex] is LiveWireLateralPath)
            {
                Pen strokePen = new Pen(new SolidColorBrush(renderInfo.HighlightedRootColors[(this.paths[this.currentHighlightedRootIndex] as LiveWireLateralPath).TargetPoint.ParentIndex]), 1.0 * CurrentUIScale);
                currentRenderDrawingContext.DrawEllipse(Brushes.White, strokePen, rootPoint, 2.5 * CurrentUIScale, 2.5 * CurrentUIScale);
            }
            else
            {
                Pen strokePen = new Pen(new SolidColorBrush(renderInfo.HighlightedRootColors[this.currentHighlightedRootIndex]), 1.0 * CurrentUIScale);
                currentRenderDrawingContext.DrawEllipse(Brushes.White, strokePen, rootPoint, 2.5 * CurrentUIScale, 2.5 * CurrentUIScale);
            }
        }

        private DrawingContext currentRenderDrawingContext = null;

        private void StartRender(DrawingContext drawingContext)
        {
            this.currentRenderDrawingContext = drawingContext;
        }

        private void EndRender()
        {
            this.currentRenderDrawingContext = null;
        }

        private bool ResetCalled { get; set; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (ResetCalled)
            {
                ResetCalled = false;
                return;
            }

            // Rendering happens in a specific order, to enforce the z-index of the drawn elements.
            base.OnRender(currentRenderDrawingContext);
            
            renderInfo.SetZoomLevel(CurrentUIScale);

            StartRender(drawingContext);

            RenderBackground();
            
            // RenderGrid();

            if (this.Stage == OverlayStage.Detection)
            {
                if (this.paths != null && this.paths.Count > 0)
                {
                    RenderRoots();

                    RenderHighlightedRoot();
                }

                RenderControlPoints();

                if (this.terminalCollection != null)
                {
                    if (this.paths == null || this.paths.Count == 0)
                    {
                        RenderLinks();
                    }

                    RenderTerminalPoints();
                }

                if (this.isControlPointDragInProgress && this.mousePosition != default(Point))
                {
                    RenderControlPointDragInfo();
                }
                else if (this.isTerminalPointDragInProgress && this.mousePosition != default(Point))
                {
                    RenderTerminalDragInfo();
                }
                else if (this.isRootDragInProgress && this.mousePosition != default(Point))
                {
                    RenderRootDragInfo();
                }

                if (this.rootPoint != default(Point) && !this.IsDragInProgress && currentHighlightedControlPointIndex < 0 && this.currentHighlightedTerminalIndex < 0)
                {
                    RenderHighlightedRootPoint();
                }

                if (this.paths != null && this.paths.Count > 0)
                {
                    RenderHighlightedControlPoint();
                }
            }
            else if (this.Stage == OverlayStage.Measurement)
            {
                if (this.roots != null && this.roots.RootTree.Count > 0)
                {
                    RenderSplineRoots();

                    RenderSplineRootInfo();
                }
            }

            EndRender();
        }

        #endregion


        public void RemoveTipHighlight()
        {
            if (this.tipHighlightAdorner != null)
            {
                AdornerLayer.GetAdornerLayer(this.AdornedElement).Remove(this.tipHighlightAdorner);
                this.tipHighlightAdorner = null;
                this.tipHighlightAdornerPosition = default(Point);
            }
        }

        public void AssignTipHighlight(Point p)
        {
            if (p == tipHighlightAdornerPosition || Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                return;
            }

            RemoveTipHighlight();

            TipHighlighter th = new TipHighlighter();

            ControlAdorner ca = new ControlAdorner(this.AdornedElement, p);
            ca.Child = new Grid();
            ca.Child.Children.Add(th);

            ScaleTransform st = new ScaleTransform(1, 1);
            th.LayoutTransform = st;

            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath("CurrentZoom");
            binding.Converter = new ZoomToScaleConverter();

            BindingOperations.SetBinding(st, ScaleTransform.ScaleXProperty, binding);
            BindingOperations.SetBinding(st, ScaleTransform.ScaleYProperty, binding);

            AdornerLayer.GetAdornerLayer(this.AdornedElement).Add(ca);
            this.tipHighlightAdorner = ca;
            this.tipHighlightAdornerPosition = p;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
        //base.OnMouseRightButtonDown(e);
        MainWindow mw = MainWindow.GetMainWindowParent(this);
        if (mw != null)
        {
            mw.ShowGMM(e.GetPosition(this));
        }
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (Stage == OverlayStage.Detection)
            {
                if (this.paths != null && this.paths.Count > 0)
                {
                    if (this.currentHighlightedControlPointIndex >= 0)
                    {
                        this.ContextMenu = new ContextMenu();
                        MenuItem removeControlPointItem = new MenuItem() { Header = "Remove Control Point" };
                        removeControlPointItem.Click += new RoutedEventHandler(removeControlPointItem_Click);
                        this.ContextMenu.Items.Add(removeControlPointItem);
                    }
                    else
                    {
                        this.ContextMenu = null;
                    }
                }
                else
                {
                    this.ContextMenu = new ContextMenu();
                    MenuItem raiseThresholdItem = new MenuItem() { Header = "Raise Threshold" };
                    raiseThresholdItem.Click += new RoutedEventHandler(raiseThresholdClick);
                    this.ContextMenu.Items.Add(raiseThresholdItem);

                    MenuItem relaxThresholdItem = new MenuItem() { Header = "Relax Threshold" };
                    relaxThresholdItem.Click += new RoutedEventHandler(relaxThresholdClick);
                    this.ContextMenu.Items.Add(relaxThresholdItem);
                }
            }
        }
         
        protected override void OnMouseMove(MouseEventArgs e)
        {
            // Check for rare event that a mouse event is fired when a drag is in progress
            if (MainWindow.GetMainWindowParent(this).FileDragInProgress)
            {
                return;
            }

            if (Stage == OverlayStage.Detection)
            {
                if (this.ContextMenu != null && this.ContextMenu.IsVisible)
                    return;
                else if (this.ContextMenu != null && !this.ContextMenu.IsVisible)
                {
                    this.ContextMenu = null;
                    return;
                }

                this.mousePosition = e.GetPosition(this);

                bool invalidateRequired = false;

                if (!this.IsDragInProgress)
                {
                    invalidateRequired = FindUIPoints(true, true, true);

                    if (this.currentHighlightedTerminalIndex >= 0)
                    {
                        // Remove any snap to tip highlights
                        RemoveTipHighlight();

                        // Check terminal and open menu if necessary
                        if (this.paths != null && this.paths.Count > 0)
                        {
                            bool terminalExists = false;
                            foreach (TerminalRootSelector trs in this.terminalRootSelectors)
                            {
                                if (trs.TerminalIndex == this.currentHighlightedTerminalIndex)
                                {
                                    trs.Cursor = Cursors.Hand;
                                    terminalExists = true;
                                    break;
                                }
                            }
                            if (!terminalExists)
                            {
                                OpenTerminalMenu();
                            }
                        }
                    }
                    else
                    {
                        List<TerminalRootSelector> allSelectors = this.terminalRootSelectors.ToList();
                        foreach (TerminalRootSelector trs in allSelectors)
                        {
                            trs.Cursor = Cursors.Arrow;
                            if (!trs.IsMouseOver && !trs.IsClosing)
                            {
                                CloseTerminalMenu(trs);
                            }
                        }

                        if (this.ToolTip != null)
                        {
                            this.ToolTip = null;
                        }

                        // Snap to Tip
                        // No terminal is currently selected, so see if we are close to one
                        if (this.SnapToTip)
                        {
                            Point position;
                            if (FindNearbyDetectedTip(mousePosition, SnapToTipThreshold, out position))
                            {
                                this.AssignTipHighlight(position);
                            }
                            else
                            {
                                RemoveTipHighlight();
                            }
                        }
                    }

                }
                else if (this.isRootDragInProgress)
                {
                    invalidateRequired = FindUIPoints(true, false, false);
                }
                else if (this.isTerminalPointDragInProgress)
                {
                    // Dragging a terminal point, check whether we are over an anchor
                    if (this.SnapToTip)
                    {
                        Point position;
                        if (FindNearbyDetectedTip(mousePosition, SnapToTipThreshold, out position))
                        {
                            this.AssignTipHighlight(position);
                        }
                        else
                        {
                            RemoveTipHighlight();
                        }
                    }
                }

                if (this.IsDragInProgress || invalidateRequired)
                {
                    this.InvalidateVisual();
                }
            }
            else if (Stage == OverlayStage.Measurement)
            {
                this.mousePosition = e.GetPosition(this);

                // If dragable point drag is in progress, do not search for other points or roots
                if (this.isMeasurementAngleDragInProgress)
                {
                    RootBase dragRoot = this.HighlightedSplineRootInfo.BaseRoot;
                    if (dragRoot.Order == 0)
                    {
                        if (measurementAnglePointType.HasValue && measurementAnglePointType.Value == RootInfo.DragType.InnerTipPoint)
                        {
                            // Find new nearest point along the spline
                            SplinePositionReference mouseReference = dragRoot.Spline.GetPositionReference(mousePosition);
                            double mouseLength = dragRoot.Spline.GetLength(mouseReference);

                            // Invert the tip distance as the TipAngleDistance is measured from the end of the spline
                            
                            double newTipAngleDistance = Math.Max(10, dragRoot.PixelLength - mouseLength);
                            newTipAngleDistance = Math.Min(newTipAngleDistance, dragRoot.Spline.Length / 2);

                            dragRoot.TipAngleDistance = newTipAngleDistance;

                            this.InvalidateVisual();
                        }
                        else if (measurementAnglePointType.HasValue && measurementAnglePointType.Value == RootInfo.DragType.MaximumAnglePoint)
                        {
                            // Find new nearest point along the spline
                            SplinePositionReference mouseReference = dragRoot.Spline.GetPositionReference(mousePosition);
                            double mouseLength = dragRoot.Spline.GetLength(mouseReference);

                            // Restrict from travelling too close to the tip
                            double newMaximumAngleDistance
                                = Math.Max(dragRoot.AngleMinimumDistance + 10, mouseLength);

                            // Restrict from travelling too close to the inner tip point
                            newMaximumAngleDistance = Math.Min(newMaximumAngleDistance, dragRoot.Spline.Length / 2);

                            dragRoot.AngleMaximumDistance = newMaximumAngleDistance;

                            this.InvalidateVisual();
                        }
                    }
                    else if (dragRoot.Order > 0)
                    {
                        if (measurementAnglePointType.HasValue && measurementAnglePointType.Value == RootInfo.DragType.InnerTipPoint)
                        {
                            // Find new nearest point along the spline
                            SplinePositionReference mouseReference = dragRoot.Spline.GetPositionReference(mousePosition);
                            double mouseLength = dragRoot.Spline.GetLength(mouseReference);

                            // Invert the tip distance as the TipAngleDistance is measured from the end of the spline
                            
                            // Restrict from travelling too close to the tip
                            double newTipAngleDistance = Math.Max(10, dragRoot.PixelLength - mouseLength);

                            // Restrict from travelling too close to the inner angle points
                            newTipAngleDistance = Math.Min(newTipAngleDistance, dragRoot.PixelLength - dragRoot.AngleMaximumDistance - 10);

                            dragRoot.TipAngleDistance = newTipAngleDistance;

                            this.InvalidateVisual();
                        }
                        else if (measurementAnglePointType.HasValue && measurementAnglePointType.Value == RootInfo.DragType.MaximumAnglePoint)
                        {
                            // Find new nearest point along the spline
                            SplinePositionReference mouseReference = dragRoot.Spline.GetPositionReference(mousePosition);
                            double mouseLength = dragRoot.Spline.GetLength(mouseReference);

                            // Invert the tip distance as the TipAngleDistance is measured from the end of the spline

                            // Restrict from travelling too close to the tip
                            double newMaximumAngleDistance
                                = Math.Max(dragRoot.AngleMinimumDistance + 10, mouseLength);

                            // Restrict from travelling too close to the inner tip point
                            newMaximumAngleDistance = Math.Min(newMaximumAngleDistance, dragRoot.PixelLength - dragRoot.TipAngleDistance - 10);

                            dragRoot.AngleMaximumDistance = newMaximumAngleDistance;

                            this.InvalidateVisual();
                        }
                        else if (measurementAnglePointType.HasValue && measurementAnglePointType.Value == RootInfo.DragType.MinimumAnglePoint)
                        {
                            // Find new nearest point along the spline
                            SplinePositionReference mouseReference = dragRoot.Spline.GetPositionReference(mousePosition);
                            double mouseLength = dragRoot.Spline.GetLength(mouseReference);

                            // Invert the tip distance as the TipAngleDistance is measured from the end of the spline

                            // Restrict from travelling too close to the source
                            double newMinimumAngleDistance = Math.Max(10, mouseLength);

                            // Restrict from travelling too close to the inner tip point
                            newMinimumAngleDistance = Math.Min(newMinimumAngleDistance, dragRoot.AngleMaximumDistance - 10);

                            dragRoot.AngleMinimumDistance = newMinimumAngleDistance;

                            this.InvalidateVisual();
                        }
                        else if (measurementAnglePointType.HasValue && measurementAnglePointType.Value == RootInfo.DragType.DualInnerPoint)
                        {
                            // Find new nearest point along the spline
                            SplinePositionReference mouseReference = dragRoot.Spline.GetPositionReference(mousePosition);
                            double mouseLength = dragRoot.Spline.GetLength(mouseReference);

                            double newMinimumAngleDistance = mouseLength - measurementAngleDualDragMinimumOffset;
                            double newMaximumAngleDistance = mouseLength + measurementAngleDualDragMaximumOffset;
                            MainWindow.GetMainWindowParent(this).Title = measurementAngleDualDragMinimumOffset.ToString();
                            // Invert the tip distance as the TipAngleDistance is measured from the end of the spline

                            // Restrict from travelling too close to the source
                            //double newMinimumAngleDistance = Math.Max(10, mouseLength);

                            // Restrict from travelling too close to the inner tip point
                            //newMinimumAngleDistance = Math.Min(newMinimumAngleDistance, dragRoot.AngleMaximumDistance - 10);

                            dragRoot.AngleMinimumDistance = newMinimumAngleDistance;
                            dragRoot.AngleMaximumDistance = newMaximumAngleDistance;

                            MainWindow.GetMainWindowParent(this).Title = (dragRoot.AngleMaximumDistance - dragRoot.AngleMinimumDistance).ToString();
                            this.InvalidateVisual();
                        }
                    }
                }
                else
                {
                    // Find a highlighted root
                    if (FindHighlightedRootFromSplines())
                    {
                        this.InvalidateVisual();
                    }

                    // Is there is a selected root, test for dragable points
                    if (this.HighlightedSplineRootInfo != null)
                    {
                        if (FindHighlightedDragPointFromSpline(5 * CurrentUIScale))
                        {
                            this.InvalidateVisual();
                        }
                        else
                        {
                            this.measurementAnglePointType = null;
                        }
                    }
                }
            }
        }

        public void fakemouseclickdown(Point fakeclick)
        {
            if (Stage == OverlayStage.Detection)
            {
                this.mousePosition = fakeclick;

                MainWindow mw = MainWindow.GetMainWindowParent(this);
                if (mw != null)
                {
                    mw.ShowGMM(fakeclick);
                }

                if (this.DetectionMode == DetectionToolbox.RootTerminalControlMode.None)
                {
                    // Initiate a drag event on a root
                    if (this.paths != null && this.currentHighlightedRootIndex >= 0)
                    {
                        if (this.ContextMenu != null && this.ContextMenu.IsVisible)
                        {
                            // We have returned from the context menu. Recalculate move event
                            // Find next highlighted root and/or control point if necessary
                            bool invalidateRequired = FindUIPoints(false, true, true);

                            if (invalidateRequired)
                            {
                                this.InvalidateVisual();
                            }

                            return;
                        }

                        // Initiate a terminal or control point drag event
                        if (!this.isRootDragInProgress)
                        {
                            if (this.currentHighlightedTerminalIndex >= 0)
                            {
                                BeginTerminalPointDrag();
                            }
                            else if (this.currentHighlightedControlPointIndex >= 0 || this.currentHighlightedRootIndex >= 0)
                            {
                                BeginControlPointDrag();
                            }
                        }
                    }
                    else if (this.currentHighlightedTerminalIndex >= 0)
                    {
                        BeginTerminalPointDrag();
                    }

                }
            }
            else if (Stage == OverlayStage.Measurement)
            {
                if (this.HighlightedSplineRootInfo != null && this.measurementAnglePointType.HasValue)
                {
                    this.isMeasurementAngleDragInProgress = true;
                }
                else
                {
                    // Select or deselect roots as necessary
                    foreach (RootBase r in this.Roots)
                    {
                        if (r.IsHighlighted)
                        {
                            if (r.IsSelected == false)
                            {
                                r.IsSelected = true;
                                this.HighlightedSplineRootInfo = new RootInfo() { BaseRoot = r };
                                this.InvalidateVisual();
                            }
                        }
                        else
                        {
                            if (r.IsSelected == true)
                            {
                                r.IsSelected = false;

                                if (this.HighlightedSplineRootInfo != null && this.HighlightedSplineRootInfo.BaseRoot == r)
                                {
                                    this.HighlightedSplineRootInfo = null;
                                }

                                this.InvalidateVisual();
                            }
                        }
                    }
                }
            }
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {

            if (Stage == OverlayStage.Detection)
            {
                this.mousePosition = e.GetPosition(this);

                MainWindow mw = MainWindow.GetMainWindowParent(this);
                if (mw != null)
                {
                    mw.ShowGMM(e.GetPosition(this));
                }

                if (this.DetectionMode == DetectionToolbox.RootTerminalControlMode.None)
                {
                    // Initiate a drag event on a root
                    if (this.paths != null && this.currentHighlightedRootIndex >= 0)
                    {
                        if (this.ContextMenu != null && this.ContextMenu.IsVisible)
                        {
                            // We have returned from the context menu. Recalculate move event
                            // Find next highlighted root and/or control point if necessary
                            bool invalidateRequired = FindUIPoints(false, true, true);

                            if (invalidateRequired)
                            {
                                this.InvalidateVisual();
                            }

                            return;
                        }

                        // Initiate a terminal or control point drag event
                        if (!this.isRootDragInProgress)
                        {
                            if (this.currentHighlightedTerminalIndex >= 0)
                            {
                                BeginTerminalPointDrag();
                            }
                            else if (this.currentHighlightedControlPointIndex >= 0 || this.currentHighlightedRootIndex >= 0)
                            {
                                BeginControlPointDrag();
                            }
                        }
                    }
                    else if (this.currentHighlightedTerminalIndex >= 0)
                    {
                        BeginTerminalPointDrag();
                    }

                }
            }
            else if (Stage == OverlayStage.Measurement)
            {
                if (this.HighlightedSplineRootInfo != null && this.measurementAnglePointType.HasValue)
                {
                    this.isMeasurementAngleDragInProgress = true;
                }
                else
                {
                    // Select or deselect roots as necessary
                    foreach (RootBase r in this.Roots)
                    {
                        if (r.IsHighlighted)
                        {
                            if (r.IsSelected == false)
                            {
                                r.IsSelected = true;
                                this.HighlightedSplineRootInfo = new RootInfo() { BaseRoot = r };
                                this.InvalidateVisual();
                            }
                        }
                        else
                        {
                            if (r.IsSelected == true)
                            {
                                r.IsSelected = false;

                                if (this.HighlightedSplineRootInfo != null && this.HighlightedSplineRootInfo.BaseRoot == r)
                                {
                                    this.HighlightedSplineRootInfo = null;
                                }

                                this.InvalidateVisual();
                            }
                        }
                    }
                }
            }
        }

        private RootTerminal previouslyAddedSource = null;

        public void fakemouseclickup(Point fakeclick)
        {
            if (Stage == OverlayStage.Detection)
            {
                Point mousePosition = fakeclick;

                Point insertPosition = mousePosition;
                if (this.tipHighlightAdornerPosition != default(Point))
                {
                    insertPosition = new Point(this.tipHighlightAdornerPosition.X, this.tipHighlightAdornerPosition.Y);
                }

                switch (this.DetectionMode)
                {
                    case DetectionToolbox.RootTerminalControlMode.AddPrimary:
                        if (this.currentHighlightedTerminalIndex >= 0 && this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Undefined)
                        {
                            this.terminalCollection[this.currentHighlightedTerminalIndex].Type = TerminalType.Primary;
                        }
                        else
                        {
                            this.terminalCollection.Add(insertPosition, TerminalType.Primary, this.LinkAdd);
                        }

                        RemoveTipHighlight();
                        FindUIPoints(true, false, false);
                        break;
                    case DetectionToolbox.RootTerminalControlMode.AddSource:
                        if (this.currentHighlightedTerminalIndex >= 0 && this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Undefined)
                        {
                            this.terminalCollection[this.currentHighlightedTerminalIndex].Type = TerminalType.Source;
                        }
                        else
                        {
                            this.terminalCollection.Add(insertPosition, TerminalType.Source, false);
                            this.previouslyAddedSource = this.terminalCollection.Last();
                        }

                        RemoveTipHighlight();
                        FindUIPoints(true, false, false);
                        break;
                    case DetectionToolbox.RootTerminalControlMode.AddLateral:
                        if (this.currentHighlightedTerminalIndex >= 0 && this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Undefined)
                        {
                            this.terminalCollection[this.currentHighlightedTerminalIndex].Type = TerminalType.Lateral;
                        }
                        else
                        {
                            this.terminalCollection.Add(insertPosition, TerminalType.Lateral, false);
                        }

                        RemoveTipHighlight();
                        FindUIPoints(true, false, false);
                        break;
                    case DetectionToolbox.RootTerminalControlMode.RemoveTerminal:
                        if (this.currentHighlightedTerminalIndex >= 0)
                        {
                            this.terminalCollection.RemoveAt(this.currentHighlightedTerminalIndex);

                            // Find any other nearby terminals, or reset the mouse cursor
                            FindUIPoints(true, false, false);
                        }
                        break;
                    case DetectionToolbox.RootTerminalControlMode.None:
                        if (this.IsDragInProgress)
                        {
                            if (this.isTerminalPointDragInProgress)
                            {
                                // End the terminal point drag event
                                EndTerminalPointDrag();
                            }
                            else if (this.isControlPointDragInProgress)
                            {
                                // End the control point drag event
                                EndControlPointDrag();
                            }
                            else if (this.isRootDragInProgress)
                            {
                                // End the root drag event
                                EndRootDrag();
                            }

                            // Find current root and/or control point under the mouse position - No need to check for invalidate visual, as it is called anyway at the end of this function
                            FindUIPoints(true, true, true);
                        }
                        break;
                }

                this.InvalidateVisual();
            }
            else if (this.Stage == OverlayStage.Measurement)
            {
                this.isMeasurementAngleDragInProgress = false;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (Stage == OverlayStage.Detection)
            {
                Point mousePosition = e.GetPosition(this);

                Point insertPosition = mousePosition;
                if (this.tipHighlightAdornerPosition != default(Point))
                {
                    insertPosition = new Point(this.tipHighlightAdornerPosition.X, this.tipHighlightAdornerPosition.Y);
                }

                switch (this.DetectionMode)
                {
                    case DetectionToolbox.RootTerminalControlMode.AddPrimary:
                        if (this.currentHighlightedTerminalIndex >= 0 && this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Undefined)
                        {
                            this.terminalCollection[this.currentHighlightedTerminalIndex].Type = TerminalType.Primary;
                        }
                        else
                        {
                            this.terminalCollection.Add(insertPosition, TerminalType.Primary, this.LinkAdd);
                        }

                        RemoveTipHighlight();
                        FindUIPoints(true, false, false);
                        break;
                    case DetectionToolbox.RootTerminalControlMode.AddSource:
                        if (this.currentHighlightedTerminalIndex >= 0 && this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Undefined)
                        {
                            this.terminalCollection[this.currentHighlightedTerminalIndex].Type = TerminalType.Source;
                        }
                        else
                        {
                            this.terminalCollection.Add(insertPosition, TerminalType.Source, false);
                            this.previouslyAddedSource = this.terminalCollection.Last();
                        }

                        RemoveTipHighlight();
                        FindUIPoints(true, false, false);
                        break;
                    case DetectionToolbox.RootTerminalControlMode.AddLateral:
                        if (this.currentHighlightedTerminalIndex >= 0 && this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Undefined)
                        {
                            this.terminalCollection[this.currentHighlightedTerminalIndex].Type = TerminalType.Lateral;
                        }
                        else
                        {
                            this.terminalCollection.Add(insertPosition, TerminalType.Lateral, false);
                        }

                        RemoveTipHighlight();
                        FindUIPoints(true, false, false);
                        break;
                    case DetectionToolbox.RootTerminalControlMode.RemoveTerminal:
                        if (this.currentHighlightedTerminalIndex >= 0)
                        {
                            this.terminalCollection.RemoveAt(this.currentHighlightedTerminalIndex);

                            // Find any other nearby terminals, or reset the mouse cursor
                            FindUIPoints(true, false, false);
                        }
                        break;
                    case DetectionToolbox.RootTerminalControlMode.None:
                        if (this.IsDragInProgress)
                        {
                            if (this.isTerminalPointDragInProgress)
                            {
                                // End the terminal point drag event
                                EndTerminalPointDrag();
                            }
                            else if (this.isControlPointDragInProgress)
                            {
                                // End the control point drag event
                                EndControlPointDrag();
                            }
                            else if (this.isRootDragInProgress)
                            {
                                // End the root drag event
                                EndRootDrag();
                            }

                            // Find current root and/or control point under the mouse position - No need to check for invalidate visual, as it is called anyway at the end of this function
                            FindUIPoints(true, true, true);
                        }
                        break;
                }

                this.InvalidateVisual();
            }
            else if (this.Stage == OverlayStage.Measurement)
            {
                this.isMeasurementAngleDragInProgress = false;
            }
        }

        void relaxThresholdClick(object sender, RoutedEventArgs e)
        {
            MainWindow.GetMainWindowParent(this).AlterCurrentGMMThreshold(true);
        }

        void raiseThresholdClick(object sender, RoutedEventArgs e)
        {
            MainWindow.GetMainWindowParent(this).AlterCurrentGMMThreshold(false);
        }

        private void OpenTerminalMenu()
        {
            RootTerminal terminal = this.terminalCollection[this.currentHighlightedTerminalIndex];
            
            // If the terminal is not a primary tip or a source, there is no need to open a terminal selecion menu
            if (terminal.Type != TerminalType.Primary && terminal.Type != TerminalType.Source)
            {
                return;
            }

            List<int> associatedRoots = new List<int>();
               
            for (int index = 0; index < this.paths.Count; index++)  
            {
                LiveWirePrimaryPath currentRoot = this.paths[index] as LiveWirePrimaryPath;

                if (currentRoot != null)
                {
                    if ((terminal.Type == TerminalType.Primary && currentRoot.TipIndex == this.currentHighlightedTerminalIndex)
                     || (terminal.Type == TerminalType.Source && currentRoot.SourceIndex == this.currentHighlightedTerminalIndex))
                    {
                        associatedRoots.Add(index);
                    }
                }
            }

            // If there are no associated roots, there is no need to open a terminal selection menu
            if (associatedRoots.Count == 0)
            {
                return;
            }

            ControlAdorner ca = new ControlAdorner(this.AdornedElement, terminal.Position);
            ca.Child = new Grid();//new Button() { HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Stretch };
            TerminalRootSelector rootSelector = new TerminalRootSelector(ca);
            rootSelector.ForceCursor = false;
            
            rootSelector.Initialize(this.currentHighlightedTerminalIndex, terminal.Type, associatedRoots, this.renderInfo);
            rootSelector.MouseLeave += new MouseEventHandler(RootSelectorMouseLeave);

            ScaleTransform st = new ScaleTransform(1, 1);
            rootSelector.LayoutTransform = st;

            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath("CurrentZoom");
            binding.Converter = new ZoomToScaleConverter();

            BindingOperations.SetBinding(st, ScaleTransform.ScaleXProperty, binding);
            BindingOperations.SetBinding(st, ScaleTransform.ScaleYProperty, binding);
            ca.Child.Children.Add(rootSelector);

            AdornerLayer.GetAdornerLayer(this.AdornedElement).Add(ca);

            rootSelector.TerminalNodeSelected += new TerminalRootSelector.TerminalNodeSelectedHandler(RootSelectorTerminalNodeSelected);
            rootSelector.MouseLeftButtonDown += new MouseButtonEventHandler(rootSelector_MouseLeftButtonDown);
            rootSelector.MouseMove += new MouseEventHandler(rootSelector_MouseMove);

            this.terminalRootAdorners.Add(ca);
            this.terminalRootSelectors.Add(rootSelector);
        }

        void rootSelector_MouseMove(object sender, MouseEventArgs e)
        {
            this.OnMouseMove(e);
        }

        void rootSelector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Close Selector
            TerminalRootSelector rootSelector = sender as TerminalRootSelector;
            if (rootSelector != null)
            {
                if (!rootSelector.IsClosing)
                {
                    CloseTerminalMenu(rootSelector);
                }
            }

            // Extra handler for mouse left button down, to prevent the root selector handling this event itself, and doing nothing.
            if (this.DetectionMode == DetectionToolbox.RootTerminalControlMode.None)
            {
                // Initiate a drag event on a root
                if (this.paths != null
                    && this.rootPoint != null
                    && this.currentHighlightedRootIndex >= 0
                    && !this.isRootDragInProgress
                    && this.currentHighlightedTerminalIndex >= 0)
                {               
                    BeginTerminalPointDrag();
                }
            }
        }

        void RootSelectorTerminalNodeSelected(object sender, TerminalType type, int rootIndex)
        {
            TerminalRootSelector selector = sender as TerminalRootSelector;
            if (selector != null)
            {
                // Begin root drag
                BeginRootDrag(type, rootIndex);

                if (!selector.IsClosing)
                {
                    CloseTerminalMenu(selector);
                }
            }
        }

        public void BeginRootDrag(TerminalType type, int rootIndex)
        {
            System.Console.WriteLine("CursorGrabbing: 790");
            Mouse.OverrideCursor = this.Grabbing;
            
            this.currentHighlightedRootIndex = rootIndex;
            isRootDragInProgress = true;

            LiveWirePrimaryPath currentPath = this.paths[rootIndex] as LiveWirePrimaryPath;

            if (currentPath == null)
            {
                System.Console.WriteLine("CursorArrow: 799");
                Mouse.OverrideCursor = Cursors.Arrow;
                
                this.currentHighlightedRootIndex = -1;
                isRootDragInProgress = false;
                return;
            }

            Point start = default(Point);

            if (type == TerminalType.Source)
            {
                if (currentPath.IntermediatePoints.Count > 0)
                {
                    start = currentPath.IntermediatePoints.First();
                }
                else
                {
                    start = this.terminalCollection[currentPath.TipIndex].Position;
                }
            }
            else if (type == TerminalType.Primary || type == TerminalType.Lateral)
            {
                if (currentPath.IntermediatePoints.Count > 0)
                {
                    start = currentPath.IntermediatePoints.Last();
                }
                else
                {
                    start = this.terminalCollection[currentPath.SourceIndex].Position;
                }
            }

            this.currentDragTerminalType = type;
            this.rootDragInfo = new RootDragInfo(start, rootIndex);
        }

        public void EndRootDrag()
        {
            try
            {
                if (this.paths != null && this.currentHighlightedTerminalIndex >= 0)
                {
                    LiveWirePrimaryPath currentPath = this.paths[this.currentHighlightedRootIndex] as LiveWirePrimaryPath;

                    if (currentPath != null)
                    {
                        int originalTerminalIndex = 0;
                        if (this.currentDragTerminalType == TerminalType.Source)
                        {
                            originalTerminalIndex = currentPath.SourceIndex;
                        }
                        else if (this.currentDragTerminalType == TerminalType.Primary || this.currentDragTerminalType == TerminalType.Lateral)
                        {
                            originalTerminalIndex = currentPath.TipIndex;
                        }


                        int newTerminalIndex = this.currentHighlightedTerminalIndex;

                        // Ensure we have a valid redirection
                        if (originalTerminalIndex == newTerminalIndex || currentDragTerminalType != currentHighlightedTerminalType)
                        {
                            // No need to redirect a root
                            return;
                        }

                        // Redirect root
                        if (this.currentHighlightedTerminalType == TerminalType.Primary)
                        {
                            currentPath.TipIndex = newTerminalIndex;
                        }
                        else
                        {
                            currentPath.SourceIndex = newTerminalIndex;
                        }

                        MainWindow.GetMainWindowParent(this).ReprocessAlteredRoot(this.currentHighlightedRootIndex);
                    }
                }
            }
            finally
            {
                // Finalise
                this.isRootDragInProgress = false;
                System.Console.WriteLine("CursorArrow: 883");
                Mouse.OverrideCursor = Cursors.Arrow;
                this.currentHighlightedRootIndex = -1;
                this.rootPoint = default(Point);
                this.rootDragInfo = null;
            }
        }

        void RootSelectorMouseLeave(object sender, MouseEventArgs e)
        {
            TerminalRootSelector selector = sender as TerminalRootSelector;

            if (selector != null && !selector.IsMouseOver && !selector.IsClosing)
            {
                CloseTerminalMenu(selector);
            }
        }

        private void CloseTerminalMenu(TerminalRootSelector sender)
        {
            sender.Closed += new EventHandler(RootSelectorClosed);
            sender.Close();
        }

        void RootSelectorClosed(object sender, EventArgs e)
        {
            TerminalRootSelector selector = sender as TerminalRootSelector;

            if (selector != null)
            {
                selector.Closed -= new EventHandler(RootSelectorClosed);
                AdornerLayer.GetAdornerLayer(this.AdornedElement).Remove(selector.ControlAdornerParent);
                this.terminalRootAdorners.Remove(selector.ControlAdornerParent);
                this.terminalRootSelectors.Remove(selector);
            }
        }

        private bool ValidTerminalConnection(LiveWirePath path)
        {
            if (path is LiveWirePrimaryPath)
            {
                LiveWirePrimaryPath p = path as LiveWirePrimaryPath;

                if ((this.currentHighlightedTerminalType == TerminalType.Primary && p.TipIndex != this.currentHighlightedTerminalIndex)
                 || (this.currentHighlightedTerminalType == TerminalType.Source && p.SourceIndex != this.currentHighlightedTerminalIndex)
                 || (this.currentHighlightedTerminalType == TerminalType.Lateral))
                {
                    return false;
                }
            }
            else if (path is LiveWireLateralPath)
            {
                LiveWireLateralPath p = path as LiveWireLateralPath;

                if (this.currentHighlightedTerminalType != TerminalType.Lateral || p.SourceIndex != this.currentHighlightedTerminalIndex)
                {
                    return false;
                }
            }
            return true;
        }

        private void BeginTerminalPointDrag()
        {
            List<Point> pathPoints = new List<Point>();
            List<int> pathIndexes = new List<int>();

            for (int index = 0; index < this.paths.Count; index++)
            {
                LiveWirePath path = this.paths[index];

                if (path == null)
                {
                    continue;
                }

                // Skip this path if it is not connected to the highlighted terminal
                if (!ValidTerminalConnection(path))
                {
                    continue;
                }

                // Find the point for the other terminal connected to this path
                Point opposingEnd = default(Point);
                if (path is LiveWirePrimaryPath)
                {
                    if (this.currentHighlightedTerminalType == TerminalType.Primary)
                    {
                        opposingEnd = this.terminalCollection[path.SourceIndex].Position;
                        if (path.IntermediatePoints != null && path.IntermediatePoints.Count > 0)
                        {
                            opposingEnd = path.IntermediatePoints.Last();
                        }
                    }
                    else if (this.currentHighlightedTerminalType == TerminalType.Source)
                    {
                        opposingEnd = this.terminalCollection[(path as LiveWirePrimaryPath).TipIndex].Position;
                        if (path.IntermediatePoints != null && path.IntermediatePoints.Count > 0)
                        {
                            opposingEnd = path.IntermediatePoints.First();
                        }
                    }
                }
                else if (path is LiveWireLateralPath)
                {
                    opposingEnd = (Point)(path as LiveWireLateralPath).TargetPoint.Point;
                }

                pathPoints.Add(opposingEnd);
                pathIndexes.Add(index);
            }

            System.Console.WriteLine("CursorGrabbing: 995");
            Mouse.OverrideCursor = this.Grabbing;
            isTerminalPointDragInProgress = true;
            
            if (pathPoints.Count > 0)
            {
                this.terminalDragInfo = new TerminalDragInfo(pathPoints, pathIndexes);
            }
        }

        private void EndTerminalPointDrag()
        {
            try
            {
                Point moveToPosition = mousePosition;

                if (this.tipHighlightAdornerPosition != default(Point))
                {
                    moveToPosition = this.tipHighlightAdornerPosition;
                    RemoveTipHighlight();
                }

                if (this.paths == null || this.paths.Count == 0)
                {
                    this.terminalCollection[this.currentHighlightedTerminalIndex].Position = moveToPosition;
                }
                else if (this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Primary
                    || this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Source)
                {
                    // Move Terminal and compile list of affected roots
                    List<int> reprocessList = new List<int>();

                    this.terminalCollection[this.currentHighlightedTerminalIndex].Position = moveToPosition;
                    for (int index = 0; index < this.paths.Count; index++)
                    {
                        LiveWirePrimaryPath currentPath = this.paths[index] as LiveWirePrimaryPath;
                        if (currentPath != null)
                        {
                            if (currentPath.TipIndex == this.currentHighlightedTerminalIndex
                                || currentPath.SourceIndex == this.currentHighlightedTerminalIndex)
                            {
                                if (!reprocessList.Contains(index))
                                {
                                    reprocessList.Add(index);
                                }
                            }
                        }
                    }
                    MainWindow.GetMainWindowParent(this).ReprocessAlteredRoot(reprocessList.ToArray());
                }
                else if (this.terminalCollection[this.currentHighlightedTerminalIndex].Type == TerminalType.Lateral)
                {
                    // Move Terminal and compile list of affected roots
                    List<int> reprocessList = new List<int>();

                    this.terminalCollection[this.currentHighlightedTerminalIndex].Position = moveToPosition;
                    for (int index = 0; index < this.paths.Count; index++)
                    {
                        LiveWireLateralPath currentPath = this.paths[index] as LiveWireLateralPath;
                        if (currentPath != null)
                        {
                            if (currentPath.SourceIndex == this.currentHighlightedTerminalIndex)
                            {
                                if (!reprocessList.Contains(index))
                                {
                                    reprocessList.Add(index);
                                }
                            }
                        }
                    }

                    MainWindow.GetMainWindowParent(this).ReprocessLateralRoot(reprocessList.ToArray());
                }

            }
            finally
            {
                // Finalise
                this.isTerminalPointDragInProgress = false;
                System.Console.WriteLine("CursorArrow: 1061");
                Mouse.OverrideCursor = Cursors.Arrow;
                this.currentHighlightedRootIndex = -1;
                this.rootPoint = default(Point);
            }
        }

        private void BeginControlPointDrag()
        {
            if (this.paths[this.currentHighlightedRootIndex] is LiveWirePrimaryPath)
            {
                LiveWirePrimaryPath currentPath = this.paths[this.currentHighlightedRootIndex] as LiveWirePrimaryPath;

                System.Console.WriteLine("CursorGrabbing: 1075");
                Mouse.OverrideCursor = this.Grabbing;
                isControlPointDragInProgress = true;

                int newIndex = -1;

                if (this.currentHighlightedControlPointIndex >= 0)
                {
                    newIndex = currentPath.Indices[currentHighlightedControlPointIndex];
                    currentPath.Indices.RemoveAt(this.currentHighlightedControlPointIndex);
                    currentPath.IntermediatePoints.RemoveAt(this.currentHighlightedControlPointIndex);
                    currentHighlightedControlPointIndex = -1;
                }
                else
                {
                    newIndex = currentPath.Path.IndexOf(rootPoint);
                }

                if (currentPath.IntermediatePoints.Count > 0)
                {
                    bool indexFound = false;
                    int index = 0;
                    for (int i = 0; i < currentPath.IntermediatePoints.Count; i++)
                    {
                        if (newIndex < currentPath.Indices[i])
                        {
                            index = i;
                            indexFound = true;
                            break;
                        }
                    }
                    if (!indexFound)
                    {
                        if (newIndex == -1)
                        {
                            this.controlPointDragInfo = new ControlPointDragInfo(this.terminalCollection[currentPath.SourceIndex].Position,
                                                                     this.terminalCollection[currentPath.TipIndex].Position,
                                                                     this.currentHighlightedRootIndex);
                        }
                        else
                        {
                            this.controlPointDragInfo = new ControlPointDragInfo(currentPath.IntermediatePoints.Last(),
                                                                     this.terminalCollection[currentPath.TipIndex].Position,
                                                                     this.currentHighlightedRootIndex);
                        }
                    }
                    else
                    {
                        this.controlPointDragInfo = new ControlPointDragInfo(index == 0 ? this.terminalCollection[currentPath.SourceIndex].Position : currentPath.IntermediatePoints[index - 1],
                                                                 index == currentPath.IntermediatePoints.Count ? this.terminalCollection[currentPath.TipIndex].Position : currentPath.IntermediatePoints[index],
                                                                 this.currentHighlightedRootIndex);
                    }

                }
                else
                {
                    this.controlPointDragInfo = new ControlPointDragInfo(this.terminalCollection[currentPath.SourceIndex].Position,
                                                             this.terminalCollection[currentPath.TipIndex].Position,
                                                             this.currentHighlightedRootIndex);
                }
            }
            else if (this.paths[this.currentHighlightedRootIndex] is LiveWireLateralPath)
            {
                LiveWireLateralPath currentPath = this.paths[this.currentHighlightedRootIndex] as LiveWireLateralPath;

                System.Console.WriteLine("CursorGrabbing: 1140");
                Mouse.OverrideCursor = this.Grabbing;
                isControlPointDragInProgress = true;

                int newIndex = -1;

                if (this.currentHighlightedControlPointIndex >= 0)
                {
                    newIndex = currentPath.Indices[currentHighlightedControlPointIndex];
                    currentPath.Indices.RemoveAt(this.currentHighlightedControlPointIndex);
                    currentPath.IntermediatePoints.RemoveAt(this.currentHighlightedControlPointIndex);
                    currentHighlightedControlPointIndex = -1;
                }
                else
                {
                    newIndex = currentPath.Path.IndexOf(rootPoint);
                }

                if (currentPath.IntermediatePoints.Count > 0)
                {
                    bool indexFound = false;
                    int index = 0;
                    for (int i = 0; i < currentPath.IntermediatePoints.Count; i++)
                    {
                        if (newIndex < currentPath.Indices[i])
                        {
                            index = i;
                            indexFound = true;
                            break;
                        }
                    }
                    if (!indexFound)
                    {
                        if (newIndex == -1)
                        {
                            this.controlPointDragInfo = new ControlPointDragInfo(this.terminalCollection[currentPath.SourceIndex].Position,
                                                                     (Point)currentPath.TargetPoint.Point,
                                                                     currentPath.TargetPoint.ParentIndex);
                        }
                        else
                        {
                            this.controlPointDragInfo = new ControlPointDragInfo(currentPath.IntermediatePoints.Last(),
                                                                     (Point)currentPath.TargetPoint.Point,
                                                                     currentPath.TargetPoint.ParentIndex);
                        }
                    }
                    else
                    {
                        this.controlPointDragInfo = new ControlPointDragInfo(index == 0 ? this.terminalCollection[currentPath.SourceIndex].Position : currentPath.IntermediatePoints[index - 1],
                                                                 index == currentPath.IntermediatePoints.Count ? (Point)currentPath.TargetPoint.Point : currentPath.IntermediatePoints[index],
                                                                 currentPath.TargetPoint.ParentIndex);
                    }

                }
                else
                {
                    this.controlPointDragInfo = new ControlPointDragInfo(this.terminalCollection[currentPath.SourceIndex].Position,
                                                             (Point)currentPath.TargetPoint.Point,
                                                             currentPath.TargetPoint.ParentIndex);
                }
            }
        }

        private void EndControlPointDrag()
        {
            // Process mouse up after drag
            MainWindow.GetMainWindowParent(this).AddControlPointToRoot(this.currentHighlightedRootIndex, this.rootPoint, this.mousePosition);

            // Finalise
            this.isControlPointDragInProgress = false;
            System.Console.WriteLine("CursorArrow: 1210");
            Mouse.OverrideCursor = Cursors.Arrow;
            this.currentHighlightedRootIndex = -1;
            this.rootPoint = default(Point);
        }

        private void removeControlPointItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentHighlightedRootIndex >= 0 && this.currentHighlightedControlPointIndex >= 0)
            {
                if (this.paths[this.currentHighlightedRootIndex] is LiveWirePrimaryPath)
                {
                    LiveWirePrimaryPath currentPath = this.paths[this.currentHighlightedRootIndex] as LiveWirePrimaryPath;

                    if (currentPath == null)
                    {
                        // Check for nearby root points following the removal of a control point
                        FindUIPoints(false, false, true);

                        System.Console.WriteLine("CursorArrow: 1319");
                        Mouse.OverrideCursor = Cursors.Arrow;

                        this.InvalidateVisual();
                        return;
                    }

                    currentPath.Indices.RemoveAt(this.currentHighlightedControlPointIndex);
                    currentPath.IntermediatePoints.RemoveAt(this.currentHighlightedControlPointIndex);

                    MainWindow.GetMainWindowParent(this).ReprocessAlteredRoot(this.currentHighlightedRootIndex);

                    this.currentHighlightedControlPointIndex = -1;

                    // This is not a mouse event, so we must re-calculate the mouse position
                    this.mousePosition = Mouse.GetPosition(this);

                    // Check for nearby root points following the removal of a control point
                    FindUIPoints(false, false, true);

                    System.Console.WriteLine("CursorArrow: 1339");
                    Mouse.OverrideCursor = Cursors.Arrow;

                    this.InvalidateVisual();
                }
                else if (this.paths[this.currentHighlightedRootIndex] is LiveWireLateralPath)
                {
                    LiveWireLateralPath currentPath = this.paths[this.currentHighlightedRootIndex] as LiveWireLateralPath;

                    if (currentPath == null)
                    {
                        // Check for nearby root points following the removal of a control point
                        FindUIPoints(false, false, true);

                        System.Console.WriteLine("CursorArrow: 1353");
                        Mouse.OverrideCursor = Cursors.Arrow;

                        this.InvalidateVisual();
                        return;
                    }

                    currentPath.Indices.RemoveAt(this.currentHighlightedControlPointIndex);
                    currentPath.IntermediatePoints.RemoveAt(this.currentHighlightedControlPointIndex);

                    MainWindow.GetMainWindowParent(this).ReprocessLateralRoot(this.currentHighlightedRootIndex);

                    this.currentHighlightedControlPointIndex = -1;

                    // This is not a mouse event, so we must re-calculate the mouse position
                    this.mousePosition = Mouse.GetPosition(this);

                    // Check for nearby root points following the removal of a control point
                    FindUIPoints(false, false, true);

                    System.Console.WriteLine("CursorArrow: 1373");
                    Mouse.OverrideCursor = Cursors.Arrow;

                    this.InvalidateVisual();
                }
            }
        }

        private bool FindUIPoints(bool examineTerminals, bool examineControlPoints, bool examineRoots)
        {
            bool terminalFound = false, controlPointFound = false, rootFound = false;

            if (examineTerminals)
            {
                terminalFound = FindHighlightedTerminal();
            }

            if (examineRoots)
            {
                rootFound = FindHighlightedRoot();
            }
  
            if (examineControlPoints)
            {
                controlPointFound = FindHighlightedControlPoint();
            }

            if (this.currentHighlightedControlPointIndex >= 0 && this.currentHighlightedRootIndex >= 0)
            {
                if (this.paths[this.currentHighlightedRootIndex].Indices.Count == 0 && this.currentHighlightedControlPointIndex >= 0)
                {

                }
            }

            return terminalFound || controlPointFound || rootFound;
        }

        private bool FindHighlightedTerminal()
        {
            int terminalIndex;

            if (FindNearbyTerminalPoint(mousePosition, TerminalUISize * CurrentUIScale, out terminalIndex))
            {
                if (this.currentHighlightedTerminalIndex != terminalIndex && !this.isTerminalPointDragInProgress && !this.isControlPointDragInProgress)
                {
                    System.Console.WriteLine("CursorHand: 1399");
                    Cursor = Cursors.Hand;
                    this.currentHighlightedTerminalIndex = terminalIndex;
                    this.currentHighlightedTerminalType = this.terminalCollection[terminalIndex].Type;
                    return true;
                }
            }
            else
            {
                if (this.currentHighlightedTerminalIndex >= 0 && !this.IsDragInProgress)
                {
                    System.Console.WriteLine("CursorArrow: 1410");
                    Cursor = Cursors.Arrow;
                    this.currentHighlightedTerminalIndex = -1;
                    return true;
                }
            }
            return false;
        }

        private bool FindNearbyDetectedTip(Point position, double distanceThreshold, out Point pos)
        {
            if (this.tipAnchorPoints.Count == 0)
            {
                pos = default(Point);
                return false;
            }

            double distance = double.MaxValue;
            int index = 0;
            for (int i = 0; i < this.tipAnchorPoints.Count; i++)
            {
                Point p = this.tipAnchorPoints[i];
                double d = Math.Sqrt(Math.Pow(p.X - mousePosition.X, 2.0) + Math.Pow(p.Y - mousePosition.Y, 2.0));
                if (d < distance)
                {
                    index = i;
                    distance = d;
                }
            }

            Point detected = this.tipAnchorPoints[index];
            
            // Check point is not also the location of a terminal
            foreach (RootTerminal terminal in this.terminalCollection)
            {
                double d = Math.Sqrt(Math.Pow(detected.X - terminal.Position.X, 2.0) + Math.Pow(detected.Y - terminal.Position.Y, 2.0));
                if (d < 2.0 && !this.isTerminalPointDragInProgress)
                {
                    pos = default(Point);
                    return false;
                }
            }

            if (distance <= distanceThreshold)
            {
                pos = detected;
                return true;
            }
            else
            {
                pos = default(Point);
                return false;
            }

        }

        private bool FindNearbyTerminalPoint(Point position, double distanceThreshold, out int terminalIndex)
        {
            double distance = double.MaxValue;  
            int index = 0;
            for(int i = 0; i < this.terminalCollection.Count; i++)
            {
                RootTerminal terminal = this.terminalCollection[i];
                Point p = terminal.Position;
                double d = Math.Sqrt(Math.Pow(p.X - mousePosition.X, 2.0) + Math.Pow(p.Y - mousePosition.Y, 2.0));
                if (d < distance)
                {
                    index = i;
                    distance = d;
                }
            }

            if (distance <= distanceThreshold)
            {
                terminalIndex = index;
                return true;
            }
            else
            {
                terminalIndex = -1;
                return false;
            }
          
        }

        private bool FindHighlightedControlPoint()
        {
            if (this.paths != null && this.paths.Count > 0)
            {
                int rootIndex;
                int controlPointIndex;

                if (FindNearbyControlPoint(mousePosition, ControlPointUISize * CurrentUIScale, out controlPointIndex, out rootIndex))
                {
                    if (this.currentHighlightedControlPointIndex != controlPointIndex && !this.IsDragInProgress)
                    {
                        Cursor = Cursors.Hand;
                        this.currentHighlightedControlPointIndex = controlPointIndex;
                        this.currentHighlightedRootIndex = rootIndex;

                        return true;
                    }
                    else
                    {
                        if (!this.IsDragInProgress)
                        {
                            // If another root index has been highlighted, override this with the currently selected control point.
                            if (this.currentHighlightedRootIndex != rootIndex)
                            {
                                this.currentHighlightedRootIndex = rootIndex;
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    if (this.currentHighlightedControlPointIndex != -1 && !this.IsDragInProgress)
                    {
                        Cursor = Cursors.Arrow;
                        this.currentHighlightedControlPointIndex = -1;

                        return true;
                    }
                }
            }

            return false;
        }

        private bool FindNearbyControlPoint(Point position, double distanceThreshold, out int controlPointIndex, out int rootIndex)
        {
            double distance = double.MaxValue;
            int currentRootIndex = 0;
            int currentControlPointIndex = 0;
            for (int currentIndex = 0; currentIndex < this.paths.Count; currentIndex++)
            {
                LiveWirePath currentRoot = this.paths[currentIndex] as LiveWirePath;

                if (currentRoot == null || currentRoot.Indices == null)
                    continue;

                for (int index = 0; index < currentRoot.Indices.Count; index++)
                {
                    int pathIndex = currentRoot.Indices[index];
                    Point controlPointPosition = currentRoot.Path[pathIndex];
                    double d2 = Math.Pow(controlPointPosition.X - position.X, 2.0) + Math.Pow(controlPointPosition.Y - position.Y, 2.0);
                    if (d2 < distance)
                    {
                        distance = d2;
                        currentRootIndex = currentIndex;
                        currentControlPointIndex = index;
                    }
                }

            }

            if (Math.Sqrt(distance) <= distanceThreshold)
            {
                controlPointIndex = currentControlPointIndex;
                rootIndex = currentRootIndex;
                return true;
            }
            else
            {
                controlPointIndex = -1;
                rootIndex = -1;
                return false;
            }
        }

        private bool FindHighlightedDragPointFromSpline(double distanceThreshold)
        {
            RootBase selectedRoot = this.HighlightedSplineRootInfo.BaseRoot;
            if (!selectedRoot.IsSelected || selectedRoot.PixelLength < 50)
            {
                return false;
            }

            List<Tuple<RootInfo.DragType, Point>> dragablePoints = new List<Tuple<RootInfo.DragType, Point>>();

            if (selectedRoot.Order > 0)
            {
                dragablePoints.Add(new Tuple<RootInfo.DragType, Point>(RootInfo.DragType.InnerTipPoint, selectedRoot.InnerTipPoint));
                dragablePoints.Add(new Tuple<RootInfo.DragType, Point>(RootInfo.DragType.MinimumAnglePoint, selectedRoot.MinimumAnglePoint));
                dragablePoints.Add(new Tuple<RootInfo.DragType, Point>(RootInfo.DragType.MaximumAnglePoint, selectedRoot.MaximumAnglePoint));
            }
            else if (selectedRoot.Order == 0)
            {
                dragablePoints.Add(new Tuple<RootInfo.DragType, Point>(RootInfo.DragType.InnerTipPoint, selectedRoot.InnerTipPoint));
                dragablePoints.Add(new Tuple<RootInfo.DragType, Point>(RootInfo.DragType.MaximumAnglePoint, selectedRoot.MaximumAnglePoint));
            }
            else
            {
                // There are no dragable points on this type of root
                return false;
            }

            // Now we have a list of candidate points, compare with the mouse position
            double distance = double.MaxValue;
            int currentPointIndex = -1;

            for (int pointIndex = 0; pointIndex < dragablePoints.Count; pointIndex++)
            {
                Point p = dragablePoints[pointIndex].Item2;

                double d2 = Math.Pow(p.X - mousePosition.X, 2.0) + Math.Pow(p.Y - mousePosition.Y, 2.0);
                if (d2 < distance)
                {
                    distance = d2;
                    currentPointIndex = pointIndex;
                }
            }

            RootInfo.DragType type = dragablePoints[currentPointIndex].Item1;

            if (distance > Math.Pow(distanceThreshold, 2) && selectedRoot.Order >= 1)
            {
                // Check to see if we can do a dual drag instead
                double p1Distance = Math.Sqrt(Math.Pow(selectedRoot.MinimumAnglePoint.X - mousePosition.X, 2.0) + Math.Pow(selectedRoot.MinimumAnglePoint.Y - mousePosition.Y, 2.0));
                double p2Distance = Math.Sqrt(Math.Pow(selectedRoot.MaximumAnglePoint.X - mousePosition.X, 2.0) + Math.Pow(selectedRoot.MaximumAnglePoint.Y - mousePosition.Y, 2.0));
                double p1p2Distance = Math.Sqrt(Math.Pow(selectedRoot.MaximumAnglePoint.X - selectedRoot.MinimumAnglePoint.X, 2.0) + Math.Pow(selectedRoot.MaximumAnglePoint.Y - selectedRoot.MinimumAnglePoint.Y, 2.0));

                if (p1Distance < p1p2Distance && p2Distance < p1p2Distance)
                {
                    this.measurementAnglePointType = RootInfo.DragType.DualInnerPoint;
                    this.measurementAngleDualDragMinimumOffset = selectedRoot.Spline.GetLength(selectedRoot.Spline.GetPositionReference(mousePosition)) - selectedRoot.AngleMinimumDistance;
                    this.measurementAngleDualDragMaximumOffset = selectedRoot.AngleMaximumDistance - selectedRoot.Spline.GetLength(selectedRoot.Spline.GetPositionReference(mousePosition));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (distance > Math.Pow(distanceThreshold, 2) && selectedRoot.Order == 0)
            {
                return false;
            }
            else
            {
                this.measurementAnglePointType = type;
                return true;
            }
        }

        private bool FindHighlightedRootFromSplines()
        {
            SplinePositionReference reference;
            RootBase highlighted;

            if (FindNearbyRootPointsFromSplines(mousePosition, RootUISize * CurrentUIScale * 3, out highlighted, out reference))
            {
                if (this.rootPositionReference == null || this.currentHighlightedRoot != highlighted)
                {
                    this.currentHighlightedRoot = highlighted;
                    this.currentHighlightedRoot.IsHighlighted = true;
                    this.rootPositionReference = reference;

                    foreach (RootBase r in this.Roots)
                    {
                        if (r.IsHighlighted && r != highlighted)
                        {
                            r.IsHighlighted = false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                if (this.rootPositionReference != null || this.currentHighlightedRoot != null)
                {
                    this.currentHighlightedRoot = null;
                    this.rootPositionReference = null;

                    foreach (RootBase r in this.roots)
                    {
                        if (r.IsHighlighted)
                        {
                            r.IsHighlighted = false;
                        }
                    }

                    return true;
                }
            }
            return false;
        }

        private bool FindNearbyRootPointsFromSplines(Point position, double distanceThreshold, out RootBase highlighted, out SplinePositionReference reference)
        {
            if (this.Roots != null && this.roots.RootTree.Count > 0)
            {
                // Find most appropriate index in sampledRoots
                double distance = double.MaxValue;
                int currentRootIndex = -1;

                List<RootBase> flattenedRoots = this.roots.ToList();

                for (int rootIndex = 0; rootIndex < flattenedRoots.Count; rootIndex++)
                {
                    RootBase r = flattenedRoots[rootIndex];

                    if (r.Order < 0)
                    {
                        continue;
                    }

                    for (int i = 0; i < r.Spline.SampledPoints.Length; i++)
                    {
                        Point p = r.Spline.SampledPoints[i];
                        double d2 = Math.Pow(p.X - position.X, 2.0) + Math.Pow(p.Y - position.Y, 2.0);
                        if (d2 < distance)
                        {
                            distance = d2;
                            currentRootIndex = rootIndex;
                        }
                    }
                }

                // Use root index to focus search in the current root paths
                if (Math.Sqrt(distance) <= distanceThreshold)
                {
                    highlighted = flattenedRoots[currentRootIndex];
                    reference = highlighted.Spline.GetPositionReference(position);
                    return true;
                }          
            }

            highlighted = null;
            reference = null;
            return false;
        }

        private bool FindHighlightedRoot()
        {
            // Assertion - It is not possible to highlight a root while adding or removing terminals
            if (this.DetectionMode != DetectionToolbox.RootTerminalControlMode.None)
            {
                if (this.currentHighlightedRootIndex != -1)
                {
                    this.currentHighlightedRootIndex = -1;
                    this.rootPoint = default(Point);
                    return true;
                }
                return false;
            }

            Point rootPosition;
            int rootIndex;     

            if (this.paths != null && this.paths.Count > 0 && this.sampledRoots != null)
            {
                if (FindNearbyRootPoints(mousePosition, RootUISize * CurrentUIScale, out rootPosition, out rootIndex))
                {
                    if (this.rootPoint != rootPosition && !this.IsDragInProgress)
                    {
                        this.currentHighlightedRootIndex = rootIndex;
                        this.rootPoint = rootPosition;
                        return true;
                    }
                }
                else
                {
                    if (this.rootPoint != default(Point) && !this.IsDragInProgress)
                    {
                        this.currentHighlightedRootIndex = -1;
                        this.rootPoint = default(Point);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool FindNearbyRootPoints(Point position, double distanceThreshold, out Point rootPosition, out int rootIndex)
        {
            // Find most appropriate index in sampledRoots
            double distance = double.MaxValue;
            int currentRootIndex = -1;
            int sampledRootPathIndex = 0;

            if (this.sampledRoots.Count == 0)
            {
                rootIndex = -1;
                rootPosition = default(Point);
                return false;
            }
           
            for (int sampleIndex = 0; sampleIndex < this.sampledRoots.Count; sampleIndex++)
            {
                List<Point> sampledRoot = this.sampledRoots[sampleIndex];
                for (int i = 0; i < sampledRoot.Count; i++)
                {
                    Point p = sampledRoot[i];
                    double d2 = Math.Pow(p.X - position.X, 2.0) + Math.Pow(p.Y - position.Y, 2.0);
                    if (d2 < distance)
                    {
                        distance = d2;
                        currentRootIndex = sampleIndex;
                        sampledRootPathIndex = i;
                    }
                }
            }

            // Use sampled roots index to focus search in the current root paths
            int currentRootPathIndexLeftBound = Math.Max(0, (sampledRootPathIndex - 1) * SampleRate);
            int currentRootPathIndexRightBound = Math.Min(this.paths[currentRootIndex].Path.Count - 1, (sampledRootPathIndex + 1) * SampleRate);
            int currentRootPathIndex = 0;
            distance = double.MaxValue;
            LiveWirePath currentRoot = this.paths[currentRootIndex];
            for (int index = currentRootPathIndexLeftBound; index <= currentRootPathIndexRightBound; index++)
            {
                Point p = currentRoot.Path[index];
                double d2 = Math.Pow(p.X - position.X, 2.0) + Math.Pow(p.Y - position.Y, 2.0);
                if (d2 < distance)
                {
                    distance = d2;
                    currentRootPathIndex = index;
                }
            }

            if (Math.Sqrt(distance) <= distanceThreshold)
            {
                rootPosition = this.paths[currentRootIndex].Path[currentRootPathIndex];
                rootIndex = currentRootIndex;
                return true;
            }
            else
            {
                rootPosition = default(Point);
                rootIndex = 0;
                return false;
            }
        }

        private class TerminalDragInfo
        {
            private List<Point> startPoints;

            public List<Point> StartPoints
            {
                get { return startPoints; }
                set { startPoints = value; }
            }

            private List<int> colorIndexes;

            public List<int> ColorIndexes
            {
                get { return colorIndexes; }
                set { colorIndexes = value; }
            }

            public TerminalDragInfo(List<Point> starts, List<int> indexes)
            {
                this.startPoints = starts;
                this.colorIndexes = indexes;
            }
        }

        private class ControlPointDragInfo
        {
            private Point startPoint;

            public Point StartPoint
            {
                get { return startPoint; }
                set { startPoint = value; }
            }
            private Point endPoint;

            public Point EndPoint
            {
                get { return endPoint; }
                set { endPoint = value; }
            }
            private int colorIndex;

            public int ColorIndex
            {
                get { return colorIndex; }
                set { colorIndex = value; }
            }

            public ControlPointDragInfo(Point start, Point end, int colorIndex)
            {
                this.startPoint = start;
                this.endPoint = end;
                this.colorIndex = colorIndex;
            }

        }

        private class RootDragInfo
        {
            private Point startPoint;

            public Point StartPoint
            {
                get { return startPoint; }
                set { startPoint = value; }
            }

            private int colorIndex;

            public int ColorIndex
            {
                get { return colorIndex; }
                set { colorIndex = value; }
            }

            public RootDragInfo (Point start, int colorIndex)
            {
                this.startPoint = start;
                this.colorIndex = colorIndex;
            }
        }
    }
    
    [ValueConversion(typeof(double), typeof(double))]
    public class ZoomToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double zoom = (double)value;
            return Math.Min(4.0, Math.Max(0.6, 1.0 / zoom));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class IsBusyVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isbusy = (bool)value;
            return isbusy ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility v = (Visibility)value;
            return v == Visibility.Visible ? false : true;
        }
    }

    public enum OverlayStage
    {
        Detection, Measurement
    }
}
