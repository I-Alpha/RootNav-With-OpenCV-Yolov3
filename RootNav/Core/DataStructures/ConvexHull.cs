// Modified version of the Graham scan algorithm implementation found in Aforge. Adapted to use this application's structures.

// AForge Math Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2007-2011
// contacts@aforgenet.com
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RootNav.Core.LiveWires;

namespace RootNav.Core.DataStructures
{
    static class ConvexHull
    {
        public static List<Int32Point> FindHull(List<Int32Point> points)
        {
            List<PointToProcess> pointsToProcess = new List<PointToProcess>();

            // convert input points to points we can process
            foreach (Int32Point point in points)
            {
                pointsToProcess.Add(new PointToProcess(point));
            }

            // find a point, with lowest X and lowest Y
            int firstCornerIndex = 0;
            PointToProcess firstCorner = pointsToProcess[0];

            for (int i = 1, n = pointsToProcess.Count; i < n; i++)
            {
                if ((pointsToProcess[i].X < firstCorner.X) ||
                     ((pointsToProcess[i].X == firstCorner.X) && (pointsToProcess[i].Y < firstCorner.Y)))
                {
                    firstCorner = pointsToProcess[i];
                    firstCornerIndex = i;
                }
            }

            // remove the just found point
            pointsToProcess.RemoveAt(firstCornerIndex);

            // find K (tangent of line's angle) and distance to the first corner
            for (int i = 0, n = pointsToProcess.Count; i < n; i++)
            {
                int dx = pointsToProcess[i].X - firstCorner.X;
                int dy = pointsToProcess[i].Y - firstCorner.Y;

                // don't need square root, since it is not important in our case
                pointsToProcess[i].Distance = dx * dx + dy * dy;
                // tangent of lines angle
                pointsToProcess[i].K = (dx == 0) ? float.PositiveInfinity : (float)dy / dx;
            }

            // sort points by angle and distance
            pointsToProcess.Sort();

            List<PointToProcess> convexHullTemp = new List<PointToProcess>();

            // add first corner, which is always on the hull
            convexHullTemp.Add(firstCorner);
            // add another point, which forms a line with lowest slope
            convexHullTemp.Add(pointsToProcess[0]);
            points.RemoveAt(0);

            PointToProcess lastPoint = convexHullTemp[1];
            PointToProcess prevPoint = convexHullTemp[0];

            while (pointsToProcess.Count != 0)
            {
                PointToProcess newPoint = pointsToProcess[0];

                // skip any point, which has the same slope as the last one or
                // has 0 distance to the first point
                if ((newPoint.K == lastPoint.K) || (newPoint.Distance == 0))
                {
                    pointsToProcess.RemoveAt(0);
                    continue;
                }

                // check if current point is on the left side from two last points
                if ((newPoint.X - prevPoint.X) * (lastPoint.Y - newPoint.Y) - (lastPoint.X - newPoint.X) * (newPoint.Y - prevPoint.Y) < 0)
                {
                    // add the point to the hull
                    convexHullTemp.Add(newPoint);
                    // and remove it from the list of points to process
                    pointsToProcess.RemoveAt(0);

                    prevPoint = lastPoint;
                    lastPoint = newPoint;
                }
                else
                {
                    // remove the last point from the hull
                    convexHullTemp.RemoveAt(convexHullTemp.Count - 1);

                    lastPoint = prevPoint;
                    prevPoint = convexHullTemp[convexHullTemp.Count - 2];
                }
            }

            // convert points back
            List<Int32Point> convexHull = new List<Int32Point>();

            foreach (PointToProcess pt in convexHullTemp)
            {
                convexHull.Add(pt.ToPoint());
            }

            return convexHull;
        }

        // Internal comparer for sorting points
        private class PointToProcess : IComparable
        {
            public int X;
            public int Y;
            public float K;
            public float Distance;

            public PointToProcess(Int32Point point)
            {
                X = point.X;
                Y = point.Y;

                K = 0;
                Distance = 0;
            }

            public int CompareTo(object obj)
            {
                PointToProcess another = (PointToProcess)obj;

                return (K < another.K) ? -1 : (K > another.K) ? 1 :
                    ((Distance > another.Distance) ? -1 : (Distance < another.Distance) ? 1 : 0);
            }

            public Int32Point ToPoint()
            {
                return new Int32Point(X, Y);
            }
        }

    }
}
