using System.Collections.Generic;
using System.Windows;

namespace RootNav.Core.Tips
{
    public static class RootIntersectionFinder
    {
        public static List<double[]> getBoundaryProfiles(double[] probabilityMap, int mapWidth, int mapHeight, RootNav.Core.MixtureModels.EMPatch e)
        {
            int width = mapWidth;
            int height = mapHeight;
            int patchWidth = e.Right - e.Left;
            int patchHeight = e.Bottom - e.Top;

            double[] topBorder = new double[patchWidth];
            double[] rightBorder = new double[patchHeight];
            double[] bottomBorder = new double[patchWidth];
            double[] leftBorder = new double[patchHeight];

            int i = 0;
            for (int x = e.Left; x < e.Right; x++)
                topBorder[i++] = probabilityMap[e.Top * width + x];

            i = 0;
            for (int y = e.Top; y < e.Bottom; y++)
                rightBorder[i++] = probabilityMap[y * width + e.Right - 1];

            i = 0;
            for (int x = e.Left; x < e.Right; x++)
                bottomBorder[i++] = probabilityMap[(e.Bottom - 1) * width + x];

            i = 0;
            for (int y = e.Top; y < e.Bottom; y++)
                leftBorder[i++] = probabilityMap[y * width + e.Left];

            return new List<double[]>() { topBorder, rightBorder, bottomBorder, leftBorder };
        }

        public static List<Point> findBorderIntersections(double[] probabilityMap, int width, int height, RootNav.Core.MixtureModels.EMPatch patch, double threshold, int windowRadius)
        {
            List<Point> intersectionPoints = new List<Point>();
            List<double[]> borders = RootIntersectionFinder.getBoundaryProfiles(probabilityMap, width, height, patch);
            for (int i = 0; i < 4; i++)
            {
                List<int> intersections = findRootIntersections(borders[i], threshold, windowRadius);
                
                // Top Right Bottom Left
                switch (i)
                {
                    case 0:
                        foreach (int pos in intersections)
                            intersectionPoints.Add(new Point(patch.Left + pos, patch.Top));
                        break;
                    case 1:
                        foreach (int pos in intersections)
                            intersectionPoints.Add(new Point(patch.Right - 1, patch.Top + pos));
                        break;
                    case 2:
                        foreach (int pos in intersections)
                            intersectionPoints.Add(new Point(patch.Left + pos, patch.Bottom - 1));
                        break;
                    case 3:
                        foreach (int pos in intersections)
                            intersectionPoints.Add(new Point(patch.Left, patch.Top + pos));
                        break;
                }
            }

            return intersectionPoints;
        }

        public static List<int> findRootIntersections(double[] normalisedList, double threshold, int windowRadius)
        {
            List<int> outputPoints = new List<int>();

            bool aboveThreshold = false;
            int length = normalisedList.Length;
            double peakMax = 0;
            int peakPosition = 0;

            for (int i = windowRadius; i < length - windowRadius; i++)
            {
                double total = 0;
                int count = 0;
                for (int w = i - windowRadius; w <= i + windowRadius; w++)
                {
                    total += normalisedList[w];
                    count++;
                }
                double movingAverage = total / count;

                if (movingAverage > threshold)
                {
                    if (aboveThreshold)
                    {
                        if (normalisedList[i] > peakMax)
                        {
                            peakMax = normalisedList[i];
                            peakPosition = i;
                        }
                    }
                    else
                    {
                        peakMax = normalisedList[i];
                        peakPosition = i;
                        aboveThreshold = true;
                    }
                }
                else
                {
                    if (aboveThreshold)
                    {
                        outputPoints.Add(peakPosition);
                        aboveThreshold = false;
                    }
                }

            }
            
            return outputPoints;
        }
    }
}
