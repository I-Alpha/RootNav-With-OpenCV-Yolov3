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

namespace RootNav.Core.LiveWires
{
    public static class LiveWirePathWeights
    {
        public static double CalculatePathMapWeight(List<Point> points, double[] probabilityMap, int width, int height)
        {
            double totalLength = 0.0;
            double probability = 0;
            foreach (Point p in points)
            {
                int index = (int)(p.Y * width + p.X);
                totalLength++;
                probability += probabilityMap[index];
            }

            return probability / totalLength;
        }

        public static void CalculatePathLengths(List<Point> points, out double Length, out double PixelLength)
        {
            Length = (points.First() - points.Last()).Length;
            PixelLength = points.Count;
            return;
        }


        public static double CalculatePathCurvatureWeight(List<Point> points, int expectedWallWidth, out List<double> angleWeights)
        {
            double wallWidthRatio = 2;
            int k = (int)Math.Round(expectedWallWidth * wallWidthRatio);
            int total = 0;
            List<double> angles = new List<double>();
            double totalCurvature = 0.0;
            int pointCount = points.Count;
            for (int i = 0; i < pointCount; i++)
            {
                if (i < k || i >= pointCount - k)
                {
                    angles.Add(1.0);
                    continue;
                }

                Vector v1 = points[i] - points[i - k];
                Vector v2 = points[i + k] - points[i];

                angles.Add(Math.Pow(1 - Math.Min(1, (Math.Abs(Vector.AngleBetween(v1, v2)) / 90)), 4));
                totalCurvature += Math.Pow(1 - Math.Min(1,(Math.Abs(Vector.AngleBetween(v1, v2)) / 90)),4); 
                total++;
            }

            angleWeights = angles;
            return totalCurvature / total;
        }

        public static double CalculateTotalWeight(LiveWireWeightDescriptor descriptor)
        {
            double lengthContribution = 0.1,
                   mapContribution = 0.3,
                   curvatureContribution = 0.6;

            return descriptor.Lengthweight * lengthContribution +
                   descriptor.MapWeight * mapContribution +
                   descriptor.CurvatureWeight * curvatureContribution;
        }
    }

    public class LiveWireWeightDescriptor
    {
        #region Properties
        private double length = 0.0;

        public double Length
        {
            get { return length; }
        }

        private double pixelLength = 0.0;

        public double PixelLength
        {
            get { return pixelLength; }
        }

        private double mapWeight = 0.0;

        public double MapWeight
        {
            get { return mapWeight; }
        }
        private double curvatureWeight = 0.0;

        public double CurvatureWeight
        {
            get { return curvatureWeight; }
        }
        private double lengthweight = 0.0;

        public double Lengthweight
        {
            get { return lengthweight; }
        }

        private List<double> angleWeights = null;

        public List<double> AngleWeights
        {
            get { return angleWeights; }
            set { angleWeights = value; }
        }

        #endregion

        public LiveWireWeightDescriptor(LiveWirePrimaryPath path, double[] probabilityMap, int width, int height)
        {
            List<Point> points = path.Path;
            LiveWirePathWeights.CalculatePathLengths(points, out this.length, out this.pixelLength);
            this.mapWeight = LiveWirePathWeights.CalculatePathMapWeight(points, probabilityMap, width, height);
            this.lengthweight = this.length / this.pixelLength;
            this.curvatureWeight = LiveWirePathWeights.CalculatePathCurvatureWeight(points, 25, out this.angleWeights);
        }
    }
}
