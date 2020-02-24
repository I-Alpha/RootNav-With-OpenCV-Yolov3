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

namespace RootNav.Interface
{
    class RootInfo
    {
        public enum DragType
        {
            MinimumAnglePoint, MaximumAnglePoint, InnerTipPoint, DualInnerPoint
        }

        public RootBase BaseRoot { get; set; }
        public DragType? DraggingType { get; set; }
        public double DualDragT { get; set; }

        public void RenderRootInfo(DrawingContext currentRenderDrawingContext, double CurrentUIScale)
        {
            if (BaseRoot.Order >= 0)
            {
                // General root info
                if (BaseRoot.Order > 0)
                {
                    #region Order > 0
                    Pen solidPen = new Pen(Brushes.White, 1.5 * CurrentUIScale);
                    Pen solidThinPen = new Pen(Brushes.White, 1.0 * CurrentUIScale);
                    LinearGradientBrush parentGradientBrush = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.Transparent, 0.1),
                                new GradientStop(Colors.White, 0.4),
                                new GradientStop(Colors.White, 0.6),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.Absolute
                    };
                    Pen parentGradientPen = new Pen(parentGradientBrush, 1.5 * CurrentUIScale);

                    LinearGradientBrush emergenceGradientBrush = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.Transparent, 0.1),
                                new GradientStop(Colors.White, 0.4),
                                new GradientStop(Colors.White, 0.6),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.Absolute
                    };
                    Pen emergenceGradientPen = new Pen(emergenceGradientBrush, 1.5 * CurrentUIScale);

                    LinearGradientBrush tipGradientBrush = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.White, 0.1),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.Absolute
                    };
                    Pen tipGradientPen = new Pen(tipGradientBrush, 1.5 * CurrentUIScale);

                    LinearGradientBrush verticalUpGradientBrush = new LinearGradientBrush()
                    {
                        StartPoint = new Point(0, 1),
                        EndPoint = new Point(0, 0),
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.White, 0.1),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.RelativeToBoundingBox
                    };
                    Pen verticalUpGradientPen = new Pen(verticalUpGradientBrush, 1.5 * CurrentUIScale);

                    LinearGradientBrush verticalDownGradientBrush = new LinearGradientBrush()
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.White, 0.1),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.RelativeToBoundingBox
                    };
                    Pen verticalDownGradientPen = new Pen(verticalDownGradientBrush, 1.5 * CurrentUIScale);

                    // Summary Vectors
                    double fadeLength = 50;

                    // Notable points
                    Tuple<Point, Point> parentPoints = BaseRoot.ParentVectorPoints;
                    Tuple<Point, Point> emergencePoints = new Tuple<Point, Point>(BaseRoot.MinimumAnglePoint, BaseRoot.MaximumAnglePoint);
                    Point intersection = Intersection(parentPoints.Item1.X, parentPoints.Item1.Y, parentPoints.Item2.X, parentPoints.Item2.Y, emergencePoints.Item1.X, emergencePoints.Item1.Y, emergencePoints.Item2.X, emergencePoints.Item2.Y);

                    // Parent
                    currentRenderDrawingContext.DrawLine(solidPen, parentPoints.Item1, parentPoints.Item2);
                    Vector parentVector = parentPoints.Item2 - parentPoints.Item1;
                    parentVector.Normalize();
                    parentVector *= fadeLength;
                    Point p1 = new Point(parentPoints.Item1.X - parentVector.X, parentPoints.Item1.Y - parentVector.Y);
                    Point p2 = new Point(parentPoints.Item2.X + parentVector.X, parentPoints.Item2.Y + parentVector.Y);
                    parentGradientBrush.StartPoint = p1;
                    parentGradientBrush.EndPoint = p2;
                    currentRenderDrawingContext.DrawLine(parentGradientPen, parentPoints.Item1, p1);
                    currentRenderDrawingContext.DrawLine(parentGradientPen, parentPoints.Item2, p2);

                    // Emergence
                    currentRenderDrawingContext.DrawLine(solidPen, emergencePoints.Item1, emergencePoints.Item2);
                    currentRenderDrawingContext.DrawLine(solidPen, emergencePoints.Item1, intersection);

                    Vector emergenceVector = emergencePoints.Item2 - emergencePoints.Item1;
                    emergenceVector.Normalize();
                    emergenceVector *= fadeLength;
                    p1 = new Point(intersection.X - emergenceVector.X, intersection.Y - emergenceVector.Y);
                    p2 = new Point(emergencePoints.Item2.X + emergenceVector.X, emergencePoints.Item2.Y + emergenceVector.Y);
                    emergenceGradientBrush.StartPoint = p1;
                    emergenceGradientBrush.EndPoint = p2;
                    currentRenderDrawingContext.DrawLine(emergenceGradientPen, emergencePoints.Item1, p1);
                    currentRenderDrawingContext.DrawLine(emergenceGradientPen, emergencePoints.Item2, p2);

                    // Tip
                    Tuple<Point, Point> tipPoints = new Tuple<Point, Point>(BaseRoot.InnerTipPoint, BaseRoot.End);
                    currentRenderDrawingContext.DrawLine(solidPen, tipPoints.Item1, tipPoints.Item2);
                    Vector tipVector = tipPoints.Item2 - tipPoints.Item1;
                    tipVector.Normalize();
                    tipVector *= fadeLength;
                    p1 = new Point(tipPoints.Item1.X - tipVector.X, tipPoints.Item1.Y - tipVector.Y);
                    p2 = new Point(tipPoints.Item2.X + tipVector.X, tipPoints.Item2.Y + tipVector.Y);
                    tipGradientBrush.StartPoint = tipPoints.Item2;
                    tipGradientBrush.EndPoint = p2;
                    currentRenderDrawingContext.DrawLine(tipGradientPen, tipPoints.Item2, p2);

                    // Vertical at Tip
                    currentRenderDrawingContext.DrawLine(verticalDownGradientPen, tipPoints.Item2, new Point(tipPoints.Item2.X, tipPoints.Item2.Y + fadeLength));

                    // Arc at tip
                    tipVector.Normalize();
                    tipVector *= (fadeLength / 3.0);
                    Point arc1 = new Point(tipPoints.Item2.X, tipPoints.Item2.Y + fadeLength / 3);
                    Point arc2 = new Point(tipPoints.Item2.X + tipVector.X, tipPoints.Item2.Y + tipVector.Y);

                    // setup the geometry object
                    PathGeometry geometry = new PathGeometry();
                    PathFigure figure = new PathFigure() { IsClosed = true };
                    geometry.Figures.Add(figure);
                    figure.StartPoint = arc1;

                    // add the arc to the geometry
                    figure.Segments.Add(new ArcSegment(arc2, new Size(fadeLength / 3, fadeLength / 3), 0, false, BaseRoot.EmergenceAngle < 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));


                    figure.Segments.Add(new LineSegment(tipPoints.Item2, false));
                    figure.Segments.Add(new LineSegment(arc1, false));

                    // draw the arc
                    currentRenderDrawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), solidThinPen, geometry);

                    // Text at Tip
                    string degree = char.ConvertFromUtf32(0x00B0);
                    FormattedText tipAngleText = new FormattedText(Math.Round(BaseRoot.TipAngle, 1) + degree.ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), 8 * CurrentUIScale, Brushes.White) { TextAlignment = TextAlignment.Center };
                    Point midPoint = new Point((arc1.X + arc2.X) / 2, (arc1.Y + arc2.Y) / 2);
                    Vector midLine = midPoint - BaseRoot.End;
                    midLine *= 2;


                    Point textPoint = BaseRoot.End + midLine - new Vector(0, tipAngleText.Height / 2);

                    if (Math.Abs(BaseRoot.TipAngle) <= 20)
                    {
                        double angle = Math.Max(25, Math.Abs(BaseRoot.TipAngle));
                        textPoint = new RotateTransform(BaseRoot.TipAngle < 0 ? angle : -angle, BaseRoot.End.X, BaseRoot.End.Y).Transform(textPoint);
                    }
                  
                    currentRenderDrawingContext.DrawText(tipAngleText, textPoint);

                    // Arc at parent
                    Vector parentIntersectionVector = intersection - parentPoints.Item2;
                    parentIntersectionVector.Normalize();
                    parentIntersectionVector *= (fadeLength / 3.0);

                    Vector emergenceIntersectionVector = intersection - emergencePoints.Item2;
                    emergenceIntersectionVector.Normalize();
                    emergenceIntersectionVector *= (fadeLength / 3.0);

                    arc1 = new Point(intersection.X - parentIntersectionVector.X, intersection.Y - parentIntersectionVector.Y);
                    arc2 = new Point(intersection.X - emergenceIntersectionVector.X, intersection.Y - emergenceIntersectionVector.Y);

                    // setup the geometry object
                    PathGeometry emergenceGeometry = new PathGeometry();
                    PathFigure emergenceFigure = new PathFigure() { IsClosed = true };
                    emergenceGeometry.Figures.Add(emergenceFigure);
                    emergenceFigure.StartPoint = arc1;

                    // add the arc to the geometry
                    emergenceFigure.Segments.Add(new ArcSegment(arc2, new Size(fadeLength / 3, fadeLength / 3), 0, false, BaseRoot.AngleWithParent < 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));

                    emergenceFigure.Segments.Add(new LineSegment(intersection, false));
                    emergenceFigure.Segments.Add(new LineSegment(arc1, false));

                    // draw the arc
                    currentRenderDrawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), solidThinPen, emergenceGeometry);

                    // Text at Parent
                    FormattedText parentAngleText = new FormattedText(Math.Round(BaseRoot.AngleWithParent, 1) + degree.ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), 8 * CurrentUIScale, Brushes.White) { TextAlignment = TextAlignment.Center };
                    midPoint = new Point((arc1.X + arc2.X) / 2, (arc1.Y + arc2.Y) / 2);

                    midLine = midPoint - intersection;

                    midLine *= 2;

                    textPoint = intersection + midLine - new Vector(0, parentAngleText.Height / 2);

                    if (Math.Abs(BaseRoot.AngleWithParent) <= 20)
                    {
                        double angle = Math.Max(25, Math.Abs(BaseRoot.AngleWithParent));
                        textPoint = new RotateTransform(BaseRoot.AngleWithParent < 0 ? angle : -angle, intersection.X, intersection.Y).Transform(textPoint);
                    }
                   
                    currentRenderDrawingContext.DrawText(parentAngleText, textPoint);
                    #endregion
                }
                else if (BaseRoot.Order == 0)
                {
                    #region Order == 0
                    Pen solidPen = new Pen(Brushes.White, 1.5 * CurrentUIScale);
                    Pen solidThinPen = new Pen(Brushes.White, 1.0 * CurrentUIScale);

                    LinearGradientBrush sourceGradientBrush = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.White, 0.1),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.Absolute
                    };
                    Pen sourceGradientPen = new Pen(sourceGradientBrush, 1.5 * CurrentUIScale);

                    LinearGradientBrush tipGradientBrush = new LinearGradientBrush()
                    {
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.White, 0.1),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.Absolute
                    };
                    Pen tipGradientPen = new Pen(tipGradientBrush, 1.5 * CurrentUIScale);

                    LinearGradientBrush verticalUpGradientBrush = new LinearGradientBrush()
                    {
                        StartPoint = new Point(0, 1),
                        EndPoint = new Point(0, 0),
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.White, 0.1),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.RelativeToBoundingBox
                    };
                    Pen verticalUpGradientPen = new Pen(verticalUpGradientBrush, 1.5 * CurrentUIScale);

                    LinearGradientBrush verticalDownGradientBrush = new LinearGradientBrush()
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1),
                        GradientStops = new GradientStopCollection()
                            {
                                new GradientStop(Colors.White, 0.1),
                                new GradientStop(Colors.Transparent, 0.9)
                            },
                        MappingMode = BrushMappingMode.RelativeToBoundingBox
                    };
                    Pen verticalDownGradientPen = new Pen(verticalDownGradientBrush, 1.5 * CurrentUIScale);

                    // Summary Vectors
                    double fadeLength = 50;

                    // Source
                    Tuple<Point, Point> sourcePoints = new Tuple<Point, Point>(BaseRoot.Start, BaseRoot.Start + BaseRoot.EmergenceVector);


                    currentRenderDrawingContext.DrawLine(solidPen, sourcePoints.Item1, sourcePoints.Item2);
                   
                    Vector sourceVector = sourcePoints.Item2 - sourcePoints.Item1;
                    sourceVector.Normalize();
                    sourceVector *= fadeLength;
                    Point p2 = new Point(sourcePoints.Item1.X + sourceVector.X, sourcePoints.Item1.Y + sourceVector.Y);

                    // Vertical at source
                    currentRenderDrawingContext.DrawLine(verticalDownGradientPen, sourcePoints.Item1, new Point(sourcePoints.Item1.X, sourcePoints.Item1.Y + fadeLength));

                    // Arc at Source
                    sourceVector.Normalize();
                    sourceVector *= (fadeLength / 3.0);
                    Point arc1 = new Point(sourcePoints.Item1.X, sourcePoints.Item1.Y + fadeLength / 3);
                    Point arc2 = new Point(sourcePoints.Item1.X + sourceVector.X, sourcePoints.Item1.Y + sourceVector.Y);

                    // setup the geometry object
                    PathGeometry geometry = new PathGeometry();
                    PathFigure figure = new PathFigure() { IsClosed = true };
                    geometry.Figures.Add(figure);
                    figure.StartPoint = arc1;

                    // add the arc to the geometry
                    figure.Segments.Add(new ArcSegment(arc2, new Size(fadeLength / 3, fadeLength / 3), 0, false, BaseRoot.EmergenceAngle < 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));


                    figure.Segments.Add(new LineSegment(sourcePoints.Item1, false));
                    figure.Segments.Add(new LineSegment(arc1, false));

                    // draw the arc
                    currentRenderDrawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), solidThinPen, geometry);

                    // Text at Source
                    string degree = char.ConvertFromUtf32(0x00B0);
                    FormattedText sourceAngleText = new FormattedText(Math.Round(BaseRoot.EmergenceAngle, 1) + degree.ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), 8 * CurrentUIScale, Brushes.White) { TextAlignment = TextAlignment.Center };
                    Point midPoint = new Point((arc1.X + arc2.X) / 2, (arc1.Y + arc2.Y) / 2);
                    Vector midLine = midPoint - BaseRoot.Start;
                    midLine *= 2;


                    Point textPoint = BaseRoot.Start + midLine - new Vector(0, sourceAngleText.Height / 2);

                    if (Math.Abs(BaseRoot.EmergenceAngle) <= 20)
                    {
                        double angle = Math.Max(25, Math.Abs(BaseRoot.EmergenceAngle));
                        textPoint = new RotateTransform(BaseRoot.EmergenceAngle < 0 ? angle : -angle, BaseRoot.Start.X, BaseRoot.Start.Y).Transform(textPoint);
                    }

                    currentRenderDrawingContext.DrawText(sourceAngleText, textPoint);

                    // Tip
                    Tuple<Point, Point> tipPoints = new Tuple<Point, Point>(BaseRoot.InnerTipPoint, BaseRoot.End);
                    currentRenderDrawingContext.DrawLine(solidPen, tipPoints.Item1, tipPoints.Item2);
                    Vector tipVector = tipPoints.Item2 - tipPoints.Item1;
                    tipVector.Normalize();
                    tipVector *= fadeLength;
                    Point p1 = new Point(tipPoints.Item1.X - tipVector.X, tipPoints.Item1.Y - tipVector.Y);
                    p2 = new Point(tipPoints.Item2.X + tipVector.X, tipPoints.Item2.Y + tipVector.Y);
                    tipGradientBrush.StartPoint = tipPoints.Item2;
                    tipGradientBrush.EndPoint = p2;
                    currentRenderDrawingContext.DrawLine(tipGradientPen, tipPoints.Item1, p2);

                    // Arc at Tip
                    tipVector.Normalize();
                    tipVector *= (fadeLength / 3.0);
                    arc1 = new Point(tipPoints.Item2.X, tipPoints.Item2.Y + fadeLength / 3);
                    arc2 = new Point(tipPoints.Item2.X + tipVector.X, tipPoints.Item2.Y + tipVector.Y);

                    // setup the geometry object
                    geometry = new PathGeometry();
                    figure = new PathFigure() { IsClosed = true };
                    geometry.Figures.Add(figure);
                    figure.StartPoint = arc1;

                    // add the arc to the geometry
                    figure.Segments.Add(new ArcSegment(arc2, new Size(fadeLength / 3, fadeLength / 3), 0, false, BaseRoot.TipAngle < 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));

                    figure.Segments.Add(new LineSegment(tipPoints.Item2, false));
                    figure.Segments.Add(new LineSegment(arc1, false));

                    // draw the arc
                    currentRenderDrawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), solidThinPen, geometry);

                    // Vertical at Tip
                    currentRenderDrawingContext.DrawLine(verticalDownGradientPen, tipPoints.Item2, new Point(tipPoints.Item2.X, tipPoints.Item2.Y + fadeLength));

                    // Text at Tip
                    degree = char.ConvertFromUtf32(0x00B0);
                    FormattedText tipAngleText = new FormattedText(Math.Round(BaseRoot.TipAngle, 1) + degree.ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Verdana"), 8 * CurrentUIScale, Brushes.White) { TextAlignment = TextAlignment.Center };
                    midPoint = new Point((arc1.X + arc2.X) / 2, (arc1.Y + arc2.Y) / 2);
                    midLine = midPoint - BaseRoot.End;
                    midLine *= 2;

                    textPoint = BaseRoot.End + midLine - new Vector(0, tipAngleText.Height / 2);

                    if (Math.Abs(BaseRoot.TipAngle) <= 20)
                    {
                        double angle = Math.Max(25, Math.Abs(BaseRoot.TipAngle));
                        textPoint = new RotateTransform(BaseRoot.TipAngle < 0 ? angle : -angle, BaseRoot.End.X, BaseRoot.End.Y).Transform(textPoint);
                    }

                    currentRenderDrawingContext.DrawText(tipAngleText, textPoint);
                    #endregion
                }

                // Dragable and fixed points
                if (BaseRoot.PixelLength >= 50)
                {
                    // Tip Point
                    currentRenderDrawingContext.DrawEllipse(Brushes.White, null, BaseRoot.End, 3 * CurrentUIScale, 3 * CurrentUIScale);

                    // Inner Tip Point
                    currentRenderDrawingContext.DrawEllipse(Brushes.White, null, BaseRoot.InnerTipPoint, 3 * CurrentUIScale, 3 * CurrentUIScale);

                    if (BaseRoot.Order > 0)
                    {
                        // Minimum Angle Point
                        currentRenderDrawingContext.DrawEllipse(Brushes.White, null, BaseRoot.MinimumAnglePoint, 3 * CurrentUIScale, 3 * CurrentUIScale);

                        // Maximum Angle Point
                        currentRenderDrawingContext.DrawEllipse(Brushes.White, null, BaseRoot.MaximumAnglePoint, 3 * CurrentUIScale, 3 * CurrentUIScale);
                    }

                    if (BaseRoot.Order == 0)
                    {
                        // Source Point
                        currentRenderDrawingContext.DrawEllipse(Brushes.White, null, BaseRoot.Start, 3 * CurrentUIScale, 3 * CurrentUIScale);

                        // Maximum Angle Point
                        currentRenderDrawingContext.DrawEllipse(Brushes.White, null, BaseRoot.MaximumAnglePoint, 3 * CurrentUIScale, 3 * CurrentUIScale);
                    }
                }

            }
        }

        private Point Intersection(
           double x1, double y1, double x2, double y2,
           double x3, double y3, double x4, double y4
         )
        {
            double d = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (d == 0) return default(Point);

            double xi = ((x3 - x4) * (x1 * y2 - y1 * x2) - (x1 - x2) * (x3 * y4 - y3 * x4)) / d;
            double yi = ((y3 - y4) * (x1 * y2 - y1 * x2) - (y1 - y2) * (x3 * y4 - y3 * x4)) / d;

            return new Point(xi, yi);
        }
    }
}
