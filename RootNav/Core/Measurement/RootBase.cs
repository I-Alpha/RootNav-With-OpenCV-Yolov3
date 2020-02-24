using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections;
using System.Linq;

using RootNav.Core.LiveWires;
using RootNav.Measurement;
using RootNav.Data;

namespace RootNav.Core.Measurement
{
    public abstract class RootBase : PlantComponent
    {
        #region Fields
        private int rootIndex = -1;

        public int RootIndex
        {
            get { return rootIndex; }
            set { rootIndex = value; }
        }

        public Point Start
        {
            get { return spline.Start; }
        }

        public Point End
        {
            get { return spline.End; }
        }

        private SplinePositionReference startReference;

        public SplinePositionReference StartReference
        {
            get { return startReference; }
            set { startReference = value; }
        }

        private SampledSpline spline;

        public SampledSpline Spline
        {
            get { return spline; }
            set { spline = value; }
        }

        private RootBase primaryParent = null;

        public RootBase PrimaryParent
        {
            get { return primaryParent; }
            set { primaryParent = value; }
        }

        private Color color;

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        private List<Int32Point> convexHullPoints = new List<Int32Point>();

        public List<Int32Point> ConvexHullPoints
        {
            get { return convexHullPoints; }
            set { convexHullPoints = value; }
        }

        private double angleMinimumDistance = 10.0;

        public double AngleMinimumDistance
        {
            get { return angleMinimumDistance; }
            set
            {
                angleMinimumDistance = value;
                base.RaisePropertyChanged("EmergenceAngle");
            }
        }

        private double angleMaximumDistance = 30.0;

        public double AngleMaximumDistance
        {
            get { return angleMaximumDistance; }
            set
            {
                angleMaximumDistance = value;
                base.RaisePropertyChanged("EmergenceAngle");
            }
        }

        private double tipAngleDistance = 40.0;

        public double TipAngleDistance
        {
            get { return tipAngleDistance; }
            set
            {
                tipAngleDistance = value;
                base.RaisePropertyChanged("TipAngle");
            }
        }
        #endregion

        public abstract double PixelLength
        {
            get;
        }

        public abstract double Length
        {
            get;
        }

        public abstract double PixelStartDistance
        {
            get;
        }

        public abstract double StartDistance
        {
            get;
        }

        public abstract double EmergenceAngle
        {
            get;
        }

        public abstract double TipAngle
        {
            get;
        }
    
        public double ConvexHullArea
        {
            get
            {
                double area = PixelConvexHullArea;
                if (this.UnitConversionFactor != 0)
                {
                    area *= (this.UnitConversionFactor * this.UnitConversionFactor);
                }
                return area;
            }
        }

        public double PixelConvexHullArea
        {
            get
            {
                if (this.convexHullPoints == null || this.convexHullPoints.Count == 0)
                {
                    return 0.0;
                }

                double area = 0.0;
                for (int i = 0; i < this.convexHullPoints.Count; i++)
                {
                    Int32Point one = this.convexHullPoints[i];
                    Int32Point two = this.convexHullPoints[(i + 1) % this.convexHullPoints.Count];

                    area += one.X * two.Y - two.X * one.Y;
                }

                return area / 2;
            }
        }

        public static Vector HorizontalVector
        {
            get
            {
                return new Vector(1, 0);
            }
        }

        public Vector TotalVector
        {
            get
            {
                return this.Spline.End - this.Spline.Start;
            }
        }

        public abstract double TotalAngle
        {
            get;
        }

        public Vector ParentVector
        {
            get
            {
                RootBase parentRoot = this.Parent as RootBase;
                if (parentRoot == null || this.startReference == null)
                {
                    return new Vector(0, 0);
                }

                // Create parent vector
                SampledSpline parentSpline = parentRoot.Spline;

                double parentIntersectionDistance = parentSpline.GetLength(this.startReference);
                double angleDistanceRadius = 20.0;

                Point parentStart = parentSpline.GetPoint(parentSpline.GetPositionReference(parentIntersectionDistance - angleDistanceRadius));
                Point parentEnd = parentSpline.GetPoint(parentSpline.GetPositionReference(parentIntersectionDistance + angleDistanceRadius));
                return parentEnd - parentStart;
            }
        }

        public Tuple<Point, Point> ParentVectorPoints
        {
            get
            {
                RootBase parentRoot = this.Parent as RootBase;
                if (parentRoot == null || this.startReference == null)
                {
                    return null;
                }

                // Create parent vector
                SampledSpline parentSpline = parentRoot.Spline;

                double parentIntersectionDistance = parentSpline.GetLength(this.startReference);
                double angleDistanceRadius = 20.0;

                Point parentStart = parentSpline.GetPoint(parentSpline.GetPositionReference(Math.Max(0, parentIntersectionDistance - angleDistanceRadius)));
                Point parentEnd = parentSpline.GetPoint(parentSpline.GetPositionReference(Math.Min(parentSpline.Length, parentIntersectionDistance + angleDistanceRadius)));
                return new Tuple<Point, Point> (parentStart, parentEnd);
            }
        }

        public abstract Vector EmergenceVector
        {
            get;
        }

        public abstract Vector TipVector
        {
            get;
        }

        public abstract Point InnerTipPoint
        {
            get;
        }

        public abstract Point MinimumAnglePoint
        {
            get;
        }

        public abstract Point MaximumAnglePoint
        {
            get;
        }

        public static RootCollection CreateRootSystem(LiveWirePathCollection pathCollection, RootTerminalCollection terminals, List<Color> colors, int splineResolution, double unitConversion)
        {
            Dictionary<int, Tuple<RootBase, int, int>> indexedRoots = new Dictionary<int, Tuple<RootBase, int, int>>();
            double tension = 0.5;
            HashSet<int> sourceIndexes = new HashSet<int>();

            for (int index = 0; index < pathCollection.Count; index++)
            {
                LiveWirePath currentPath = pathCollection[index];

                if (currentPath is LiveWirePrimaryPath)
                {
                    LiveWirePrimaryPath primaryLiveWire = currentPath as LiveWirePrimaryPath;

                    RootBase primary = new PrimaryRoot() { UnitConversionFactor = unitConversion };
                    primary.RootIndex = index;

                    primary.Spline = new SampledSpline() { ControlPointSeparation = splineResolution, Tension = tension };
                    primary.Spline.Initialise(primaryLiveWire.Path);

                    primary.Color = colors[index];

                    indexedRoots.Add(index, new Tuple<RootBase, int, int> (primary, primaryLiveWire.SourceIndex, -1));

                    sourceIndexes.Add(primaryLiveWire.SourceIndex);

                }
                else if (currentPath is LiveWireLateralPath)
                {
                    LiveWireLateralPath lateralLiveWire = currentPath as LiveWireLateralPath;

                    RootBase lateral = new LateralRoot() { UnitConversionFactor = unitConversion };
                    lateral.RootIndex = index;
                    List<Point> path = lateralLiveWire.Path.ToList();
                    path.Reverse();
                    lateral.Spline = new SampledSpline() { ControlPointSeparation = splineResolution, Tension = tension };
                    lateral.Spline.Initialise(path);

                    lateral.Color = colors[lateralLiveWire.TargetPoint.ParentIndex];

                    indexedRoots.Add(index, new Tuple<RootBase, int, int> (lateral, -1, lateralLiveWire.TargetPoint.ParentIndex));
                }
            }

            Dictionary<int, RootBase> plants = new Dictionary<int,RootBase>();
            foreach (int i in sourceIndexes)
            {
                RootBase plant = new RootGroup() { UnitConversionFactor = unitConversion };
                plants.Add(i, plant);
            }

            // Create tree structure based on references
            List<RootBase> primaryRoots = new List<RootBase>();
            foreach (var pair in indexedRoots)
            {
                RootBase r = pair.Value.Item1;
                int plantSourceIndex = pair.Value.Item2;
                int parentIndex = pair.Value.Item3;

                if (parentIndex == -1)
                {
                    // Add primary root to main list
                    plants[plantSourceIndex].Children.Add(r);
                }
                else
                {
                    // Add lateral to parent's children
                    indexedRoots[parentIndex].Item1.Children.Add(r);
                    r.Parent = indexedRoots[parentIndex].Item1;
                }
            }

            List<RootBase> plantList = plants.Values.ToList();

            int plantIndex = 1;
            foreach (RootBase plant in plantList)
            {
                plant.ID = plantIndex++;

                // Lateral angles can now be calculated
                MarkStartReferences(plant.Children);

                // Calculate IDs
                MarkIDs(plant.Children);

                // Mark primary parents
                MarkPrimaryParents(plant, plant.Children);

                // Mark convex hulls
                MarkConvexHulls(plant.Children);

                // Plant settings
                plant.Color = Color.FromArgb(255, 200, 200, 200);

                // Plant hull
                SetPlantHullPoints(plant);
            }

            // Create plant systems
            return new RootCollection { RootTree = plantList };
        }

        private static void SetPlantHullPoints(RootBase r)
        {
            List<Int32Point> samplePoints = new List<Int32Point>();
            foreach (RootBase child in r.Children)
            {
                samplePoints.AddRange(child.ConvexHullPoints);
            }

            r.convexHullPoints = RootNav.Core.DataStructures.ConvexHull.FindHull(samplePoints);

        }

        public abstract double AngleWithParent
        {
            get;
        }

        private static void MarkConvexHulls(List<PlantComponent> roots)
        {
            foreach (RootBase r in roots)
            {
                // Calculate and mark convex hull
                if (r.Order == 0)
                {
                    List<Point> samplePoints = new List<Point>();
                    samplePoints.AddRange(r.Spline.SampledPoints);
                    foreach (RootBase child in r.Children)
                    {
                        samplePoints.AddRange(child.Spline.SampledPoints);
                    }

                    List<Int32Point> intPoints = new List<Int32Point>();
                    foreach (Point s in samplePoints)
                    {
                        intPoints.Add((Int32Point)s);
                    }

                    r.convexHullPoints = RootNav.Core.DataStructures.ConvexHull.FindHull(intPoints);
                }
            }
        }

        private static void MarkStartReferences(List<PlantComponent> roots)
        {
            foreach (RootBase r in roots)
            {
                RootBase parentRoot = r.Parent as RootBase;
                if (r.Parent != null)
                {
                    r.startReference = parentRoot.Spline.GetPositionReference(r.Start);
                }
                if (r.Children.Count > 0)
                {
                    MarkStartReferences(r.Children);
                }
            }
        }

        private static void MarkIDs(List<PlantComponent> roots)
        {
            int count = 1;
            foreach (RootBase r in roots)
            {
                r.ID = count++;
                if (r.Children.Count > 0)
                {
                    MarkIDs(r.Children);
                }
            }
        }

        private static void MarkPrimaryParents(PlantComponent primaryParent, List<PlantComponent> roots)
        {
            foreach (RootBase r in roots)
            {
                r.PrimaryParent = null;
                r.Parent = primaryParent;
                MarkLateralParents(r, r, r.Children);
            }
        }

        private static void MarkLateralParents(PlantComponent primaryParent, PlantComponent parent, List<PlantComponent> roots)
        {
            foreach (RootBase r in roots)
            {
                r.PrimaryParent = primaryParent as RootBase;
                r.Parent = parent;
                MarkLateralParents(primaryParent, r, r.Children);
            }
        }

        public Tuple<double, double>[] GetCurvatureProfile(double resolution)
        {
            if (this.Order < 0)
            {
                return null;
            }

            // Resolution here is measured in pixels or mm depending on if there's a conversion available
            if (this.UnitConversionFactor != 0)
            {
                // This converts resolution back to pixels in all circumstances
                resolution /= this.UnitConversionFactor;
            }

            // The distance in pixels either side of some point i to use for the curvature calculation
            double k = 12.0;

            List<Tuple<double, double>> outputList = new List<Tuple<double, double>>();
            // Obtain the curvature profile at the specified resolution
            double rootLength = this.Spline.Length;
            for (double i = k; i < rootLength - k; i += resolution)
            {
                // Obtain points i - k, i, i + k
                Point p0 = this.Spline.GetPoint(this.Spline.GetPositionReference(i - k));
                Point p1 = this.Spline.GetPoint(this.Spline.GetPositionReference(i));
                Point p2 = this.Spline.GetPoint(this.Spline.GetPositionReference(i + k));

                double angle = Vector.AngleBetween(p1 - p0, p2 - p1);

                if (this.UnitConversionFactor == 0) 
                {
                    // Index in pixels
                    outputList.Add(new Tuple<double,double>(i, angle));
                }
                else
                {
                    // Index in mm
                    outputList.Add(new Tuple<double,double>(this.UnitConversionFactor * i, angle));
                }
            }

            return outputList.ToArray();
        }

        public void GetMapProfile(double resolution, double travel, double[] probabilityMap, int width, int height, out Tuple<double, double>[] leftData, out Tuple<double, double>[] rightData)
        {
            if (this.Order < 0)
            {
                leftData = null;
                rightData = null;
                return;
            }

            // Resolution here is measured in pixels or mm depending on if there's a conversion available
            if (this.UnitConversionFactor != 0)
            {
                // This converts resolution back to pixels in all circumstances
                resolution /= this.UnitConversionFactor;
            }

            // The distance in pixels either side of some point i to use for the curvature calculation
            double k = 3.0;

            List<Tuple<double, double>> leftList = new List<Tuple<double, double>>();
            List<Tuple<double, double>> rightList = new List<Tuple<double, double>>();

            // Obtain the curvature profile at the specified resolution
            double rootLength = this.Spline.Length;
            for (double i = k; i < rootLength - k; i += resolution)
            {
                // Obtain points i - k, i, i + k
                Point p0 = this.Spline.GetPoint(this.Spline.GetPositionReference(i - k));
                Point p1 = this.Spline.GetPoint(this.Spline.GetPositionReference(i));
                Point p2 = this.Spline.GetPoint(this.Spline.GetPositionReference(i + k));

                // This is the vector of the spline at this position
                Vector splineVector = p0 - p2;

                // Normals in both directions
                Vector CWvector = new Vector(splineVector.Y, -splineVector.X);
                Vector CCWvector = new Vector(-splineVector.Y, splineVector.X);
                
                // Bresenham in both directions
                for (int a = 0; a <= 1; a++)
                {
                    double totalIntensity = 0.0;

                    Point source = p1;
                    
                    Vector destinationVector = a == 0 ? CWvector : CCWvector;

                    // k * travel simply makes the vector large enough for any reasonable walk. We will never actually travel this distance.
                    Point destination = source + (destinationVector * travel);

                    // Calculate whether the CW or CCW is on the left or the right
                    bool left = isLeft(p0, p2, destination);

                    double dx = Math.Abs(destination.X - source.X);
                    double dy = Math.Abs(destination.Y - source.Y);

                    int sx = source.X < destination.X ? 1 : -1;
                    int sy = source.Y < destination.Y ? 1 : -1;

                    double err = dx - dy;
                    double travelSquared = travel * travel;
                    int x = (int)source.X,
                        y = (int)source.Y,
                        fx = (int)destination.X,
                        fy = (int)destination.Y;

                    while (true)
                    {
                        if (x == fx && y == fy)
                            break;

                        double currentDistanceSquared = Math.Pow(source.X - x, 2) + Math.Pow(source.Y - y, 2);

                        if (currentDistanceSquared > travelSquared)
                            break;

                        if (!(x < 0 || x >= width || y < 0 || y >= height))
                        {
                            // Use (x, y) Position
                            totalIntensity += probabilityMap[y * width + x];
                        }

                        // Next pixel
                        double err2 = 2 * err;
                        if (err2 > -dy)
                        {
                            err -= dy;
                            x += sx;
                        }
                        if (err2 < dx)
                        {
                            err += dx;
                            y += sy;
                        }
                    }


                    if (this.UnitConversionFactor == 0)
                    {
                        // Index in pixels
                        if (left)
                        {
                            leftList.Add(new Tuple<double, double>(i, totalIntensity));
                        }
                        else
                        {
                            rightList.Add(new Tuple<double, double>(i, totalIntensity));
                        }
                    }
                    else
                    {
                        // Index in mm
                        if (left)
                        {
                            leftList.Add(new Tuple<double, double>(this.UnitConversionFactor * i, totalIntensity));
                        }
                        else
                        {
                            rightList.Add(new Tuple<double, double>(this.UnitConversionFactor * i, totalIntensity));
                        }
                    }
                }




            }

            leftData = leftList.ToArray();
            rightData = rightList.ToArray();
            return;
        }

        private bool isLeft(Point a, Point b, Point c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) > 0;
        }

    }

    [ValueConversion(typeof(Color), typeof(SolidColorBrush))]
    public class UIColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color c = (Color)value;
            Color output = new Color() { A = 255, R = c.R, G = c.G, B = c.B };
            return new SolidColorBrush(output);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Colors.Black;
        }
    }

    [ValueConversion(typeof(Point), typeof(String))]
    public class PointStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Point p = (Point)value;
            return Math.Round(p.X).ToString() + ", " + Math.Round(p.Y).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return default(Point);
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class RoundDoubleConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty DPProperty =
         DependencyProperty.Register("DP", typeof(int), typeof(RoundDoubleConverter), new PropertyMetadata(2));

        public int DP
        {
            get
            {
                return (int)GetValue(DPProperty);
            }
            set
            {
                SetValue(DPProperty, value);
            }
        }


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Math.Round((double)value, DP);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
