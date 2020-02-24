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

using RootNav.Interface.Windows;
using RootNav.Core.LiveWires;


namespace RootNav.Interface.Controls
{
    public class ScreenOverlayRenderInfo
    {
        private const double controlPointDragLineRenderThickness = 1.0;
        private const double controlPointDragNodeStrokeRenderThickness = 1.0;
        private const double controlPointDragNodeRenderSize = 4.0;
        private const double controlPointHighlightEllipseRenderSize = 5.0;
        private const double controlPointEllipseRenderSize = 2.5;
        private const double controlPointStrokeRenderThickness = 1.0;
        private const double terminalTextFontRenderSize = 10.0;
        private const double terminalHighlightEllipseRenderSize = 14.0;
        private const double terminalStrokeRenderThickness = 2.0;
        private const double terminalEllipseRenderSize = 8.0;
        private const double rootRenderThickness = 2.5;
        private const double gridLineRenderThickness = 1.0;
        private const double convexHullStrokeThickness = 1.0;

        private Brush controlPointDragLineBrush = null;
        private Brush ControlPointDragLineBrush
        {
            get
            {
                if (controlPointDragLineBrush == null)
                {
                    controlPointDragLineBrush = new SolidColorBrush(Colors.White);
                    if (controlPointDragLineBrush.CanFreeze)
                    {
                        controlPointDragLineBrush.Freeze();
                    }
                }
                return controlPointDragLineBrush;
            }
        }

        private Brush terminalSourceStrokeBrush = null;

        private Brush TerminalSourceStrokeBrush
        {
            get
            {
                if (terminalSourceStrokeBrush == null)
                {
                    terminalSourceStrokeBrush = new SolidColorBrush(Color.FromArgb(255, 150, 150, 255));
                    if (terminalSourceStrokeBrush.CanFreeze)
                    {
                        terminalSourceStrokeBrush.Freeze();
                    }
                }
                return terminalSourceStrokeBrush;
            }
        }

        private Brush terminalPrimaryStrokeBrush = null;

        private Brush TerminalPrimaryStrokeBrush
        {
            get
            {
                if (terminalPrimaryStrokeBrush == null)
                {
                    terminalPrimaryStrokeBrush = new SolidColorBrush(Color.FromArgb(255, 255, 100, 100));
                    if (terminalPrimaryStrokeBrush.CanFreeze)
                    {
                        terminalPrimaryStrokeBrush.Freeze();
                    }
                }
                return terminalPrimaryStrokeBrush;
            }
        }

        private Brush terminalLateralStrokeBrush = null;

        private Brush TerminalLateralStrokeBrush
        {
            get
            {
                if (terminalLateralStrokeBrush == null)
                {
                    terminalLateralStrokeBrush = new SolidColorBrush(Color.FromArgb(255, 230, 230, 100));
                    if (terminalLateralStrokeBrush.CanFreeze)
                    {
                        terminalLateralStrokeBrush.Freeze();
                    }
                }
                return terminalLateralStrokeBrush;
            }
        }

        private Brush terminalUndefinedStrokeBrush = null;

        private Brush TerminalUndefinedStrokeBrush
        {
            get
            {
                if (terminalUndefinedStrokeBrush == null)
                {
                    terminalUndefinedStrokeBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                    if (terminalUndefinedStrokeBrush.CanFreeze)
                    {
                        terminalUndefinedStrokeBrush.Freeze();
                    }
                }
                return terminalUndefinedStrokeBrush;
            }
        }

        private Brush terminalBackgroundBrush = null;

        public Brush TerminalBackgroundBrush
        {
            get
            {
                if (terminalBackgroundBrush == null)
                {
                    terminalBackgroundBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
                    if (terminalBackgroundBrush.CanFreeze)
                    {
                        terminalBackgroundBrush.Freeze();
                    }
                }
                return terminalBackgroundBrush;
            }
        }

        private Brush gridLineBrush = null;

        private Brush GridLineBrush
        {
            get
            {
                if (gridLineBrush == null)
                {
                    gridLineBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    if (gridLineBrush.CanFreeze)
                    {
                        gridLineBrush.Freeze();
                    }
                }
                return gridLineBrush;
            }
        }

        private Brush convexHullStrokeBrush = null;

        private Brush ConvexHullStrokeBrush
        {
            get
            {
                if (convexHullStrokeBrush == null)
                {
                    convexHullStrokeBrush = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255));
                    if (convexHullStrokeBrush.CanFreeze)
                    {
                        convexHullStrokeBrush.Freeze();
                    }
                }
                return convexHullStrokeBrush;
            }
        }

        private Brush convexHullFillBrush = null;

        public Brush ConvexHullFillBrush
        {
            get
            {
                if (convexHullFillBrush == null)
                {
                    convexHullFillBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    if (convexHullFillBrush.CanFreeze)
                    {
                        convexHullFillBrush.Freeze();
                    }
                }
                return convexHullFillBrush;
            }
        }

        private RadialGradientBrush terminalHighlightEllipseBrush = null;

        public RadialGradientBrush TerminalHighlightEllipseBrush
        {
            get
            {
                if (terminalHighlightEllipseBrush == null)
                {
                    terminalHighlightEllipseBrush = new RadialGradientBrush();
                    terminalHighlightEllipseBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.45));
                    terminalHighlightEllipseBrush.GradientStops.Add(new GradientStop(Color.FromArgb(180, 255, 255, 255), 0.6));
                    terminalHighlightEllipseBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));

                    if (terminalHighlightEllipseBrush.CanFreeze) { terminalHighlightEllipseBrush.Freeze(); }
                }
                return terminalHighlightEllipseBrush;
            }
        }

        private RadialGradientBrush controlPointHighlightEllipseBrush = null;

        public RadialGradientBrush ControlPointHighlightEllipseBrush
        {
            get
            {
                if (controlPointHighlightEllipseBrush == null)
                {
                    controlPointHighlightEllipseBrush = new RadialGradientBrush();
                    controlPointHighlightEllipseBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.6));
                    controlPointHighlightEllipseBrush.GradientStops.Add(new GradientStop(Color.FromArgb(180, 255, 255, 255), 0.6));
                    controlPointHighlightEllipseBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));

                    if (controlPointHighlightEllipseBrush.CanFreeze) { controlPointHighlightEllipseBrush.Freeze(); }
                }
                return controlPointHighlightEllipseBrush;
            }
        }

        private double CurrentZoom = 1.0;

        private Pen controlPointDragLineStrokePen;

        public Pen ControlPointDragLineStrokePen
        {
            get { return controlPointDragLineStrokePen; }
        }

        private FormattedText terminalSourceText;

        public FormattedText TerminalSourceText
        {
            get { return terminalSourceText; }
        }
        private FormattedText terminalPrimaryText;

        public FormattedText TerminalPrimaryText
        {
            get { return terminalPrimaryText; }
        }

        private FormattedText terminalLateralText;

        public FormattedText TerminalLateralText
        {
            get { return terminalLateralText; }
        }

        private Pen terminalSourceStrokePen;

        public Pen TerminalSourceStrokePen
        {
            get { return terminalSourceStrokePen; }
        }
        private Pen terminalPrimaryStrokePen;

        public Pen TerminalPrimaryStrokePen
        {
            get { return terminalPrimaryStrokePen; }
        }

        private Pen terminalLateralStrokePen;

        public Pen TerminalLateralStrokePen
        {
            get { return terminalLateralStrokePen; }
        }

        private Pen terminalUndefinedStrokePen;

        public Pen TerminalUndefinedStrokePen
        {
            get { return terminalUndefinedStrokePen; }
        }

        private Pen gridLinePen;

        public Pen GridLinePen
        {
            get { return gridLinePen; }
            set { gridLinePen = value; }
        }

        private Pen convexHullPen;

        public Pen ConvexHullPen
        {
            get { return convexHullPen; }
            set { convexHullPen = value; }
        }

        // Root Pen?
        // Highlight root pen?

        public ScreenOverlayRenderInfo()
        {
            Initialize();
        }

        public void SetZoomLevel(double newZoom)
        {
            if (this.CurrentZoom != newZoom)
            {
                this.CurrentZoom = newZoom;
                Initialize();
            }
        }

        private void Initialize()
        {
            controlPointDragLineStrokePen = new Pen(ControlPointDragLineBrush, controlPointDragLineRenderThickness * CurrentZoom);
            if (controlPointDragLineStrokePen.CanFreeze)
            {
                controlPointDragLineStrokePen.Freeze();
            }

            if (terminalSourceText == null)
            {
                terminalSourceText = new FormattedText("S", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), terminalTextFontRenderSize * CurrentZoom, Brushes.White) { TextAlignment = TextAlignment.Center };
            }
            else
            {
                terminalSourceText.SetFontSize(terminalTextFontRenderSize * CurrentZoom);
            }

            if (terminalPrimaryText == null)
            {
                terminalPrimaryText = new FormattedText("P", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), terminalTextFontRenderSize * CurrentZoom, Brushes.White) { TextAlignment = TextAlignment.Center };
            }
            else
            {
                terminalPrimaryText.SetFontSize(terminalTextFontRenderSize * CurrentZoom);
            }

            if (terminalLateralText == null)
            {
                terminalLateralText = new FormattedText("L", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), terminalTextFontRenderSize * CurrentZoom, Brushes.White) { TextAlignment = TextAlignment.Center };
            }
            else
            {
                terminalLateralText.SetFontSize(terminalTextFontRenderSize * CurrentZoom);
            }

            terminalSourceStrokePen = new Pen(TerminalSourceStrokeBrush, terminalStrokeRenderThickness * CurrentZoom);
            if (terminalSourceStrokePen.CanFreeze) { terminalSourceStrokePen.Freeze(); }
            
            terminalPrimaryStrokePen = new Pen(TerminalPrimaryStrokeBrush, terminalStrokeRenderThickness * CurrentZoom);
            if (terminalPrimaryStrokePen.CanFreeze) { terminalPrimaryStrokePen.Freeze(); }

            terminalLateralStrokePen = new Pen(TerminalLateralStrokeBrush, terminalStrokeRenderThickness * CurrentZoom);
            if (terminalLateralStrokePen.CanFreeze) { terminalLateralStrokePen.Freeze(); }

            terminalUndefinedStrokePen = new Pen(TerminalUndefinedStrokeBrush, terminalStrokeRenderThickness * CurrentZoom * 0.6) { DashStyle = DashStyles.Dash };
            if (terminalUndefinedStrokePen.CanFreeze) { terminalUndefinedStrokePen.Freeze(); }

            gridLinePen = new Pen(GridLineBrush, gridLineRenderThickness * CurrentZoom);
            if (gridLinePen.CanFreeze) { gridLinePen.Freeze(); }

            convexHullPen = new Pen(ConvexHullStrokeBrush, convexHullStrokeThickness * CurrentZoom);
            if (convexHullPen.CanFreeze) { convexHullPen.Freeze(); }

            InitializeRootPens();          
        }

        public static void GenerateRootColors(int count, out List<Color> rootColors, out List<Color> highlightedRootColors)
        {
            Random r = new Random(683714);  // Unchanging "random" for extended colour palette.
            rootColors = new List<Color>();
            highlightedRootColors = new List<Color>();

            for (int x = 0; x < count; x++)
            {
                switch (x)
                {
                    case 0:
                        rootColors.Add(Color.FromArgb(100, 255, 0, 0));
                        highlightedRootColors.Add(Color.FromArgb(255, 255, 0, 0));
                        break;
                    case 1:
                        rootColors.Add(Color.FromArgb(100, 0, 0, 255));
                        highlightedRootColors.Add(Color.FromArgb(255, 0, 0, 255));
                        break;
                    case 2:
                        rootColors.Add(Color.FromArgb(100, 0, 255, 0));
                        highlightedRootColors.Add(Color.FromArgb(255, 0, 255, 0));
                        break;
                    case 3:
                        rootColors.Add(Color.FromArgb(100, 255, 255, 0));
                        highlightedRootColors.Add(Color.FromArgb(255, 255, 255, 0));
                        break;
                    case 4:
                        rootColors.Add(Color.FromArgb(100, 255, 0, 255));
                        highlightedRootColors.Add(Color.FromArgb(255, 255, 0, 255));
                        break;
                    case 5:
                        rootColors.Add(Color.FromArgb(100, 0, 255, 255));
                        highlightedRootColors.Add(Color.FromArgb(255, 0, 255, 255));
                        break;
                    case 6:
                        rootColors.Add(Color.FromArgb(100, 255, 128, 0));
                        highlightedRootColors.Add(Color.FromArgb(255, 255, 128, 0));
                        break;
                    case 7:
                        rootColors.Add(Color.FromArgb(100, 128, 0, 255));
                        highlightedRootColors.Add(Color.FromArgb(255, 128, 0, 255));
                        break;
                    case 8:
                        rootColors.Add(Color.FromArgb(100, 255, 0, 128));
                        highlightedRootColors.Add(Color.FromArgb(255, 255, 0, 128));
                        break;
                    case 9:
                        rootColors.Add(Color.FromArgb(100, 128, 255, 0));
                        highlightedRootColors.Add(Color.FromArgb(255, 128, 255, 0));
                        break;
                    case 10:
                        rootColors.Add(Color.FromArgb(100, 0, 255, 128));
                        highlightedRootColors.Add(Color.FromArgb(255, 0, 255, 128));
                        break;
                    case 11:
                        rootColors.Add(Color.FromArgb(100, 0, 128, 255));
                        highlightedRootColors.Add(Color.FromArgb(255, 0, 128, 255));
                        break;
                    default:
                        Color C = Color.FromArgb(100, (byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255));
                        Color hC = new Color() { R = C.R, G = C.G, B = C.B, A = 255 };
                        rootColors.Add(C);
                        highlightedRootColors.Add(hC);
                        break;
                }
            }
        }

        private List<Color> rootColors = null;

        public List<Color> RootColors
        {
            get { return rootColors; }
        }
        private List<Color> highlightedRootColors = null;

        public List<Color> HighlightedRootColors
        {
            get { return highlightedRootColors; }
        }


        private List<Pen> rootPens = null;

        public List<Pen> RootPens
        {
            get { return rootPens; }
        }

        private List<Pen> highlightedRootPens = null;

        public List<Pen> HighlightedRootPens
        {
            get { return highlightedRootPens; }
        }

        private int rootCount = 0;

        public int RootCount
        {
            get { return rootCount; }
            set
            {
                rootCount = value;
                InitializeRootPens();
            }
        }

        private void InitializeRootPens()
        {
            // If colour palette is already generated, no need to repeat that process
            if (rootColors == null || highlightedRootColors == null || rootColors.Count != rootCount || highlightedRootColors.Count != rootCount)
                GenerateRootColors(rootCount, out rootColors, out highlightedRootColors);

            rootPens = new List<Pen>();
            foreach (Color C in rootColors)
            {
                Brush b = new SolidColorBrush(C);
                if (b.CanFreeze) { b.Freeze(); }
                Pen p = new Pen(b, rootRenderThickness * CurrentZoom) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                if (p.CanFreeze) { p.Freeze(); }
                rootPens.Add(p);
            }

            highlightedRootPens = new List<Pen>();
            foreach (Color C in highlightedRootColors)
            {
                Brush b = new SolidColorBrush(C);
                if (b.CanFreeze) { b.Freeze(); }
                Pen p = new Pen(b, rootRenderThickness * CurrentZoom) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                if (p.CanFreeze) { p.Freeze(); }
                highlightedRootPens.Add(p);
            }
        }
    }
}
