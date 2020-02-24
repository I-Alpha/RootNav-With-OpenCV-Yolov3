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

using RootNav.Core.MixtureModels;
using RootNav.Interface.Controls;
using RootNav.Core.LiveWires;
using RootNav.Core.DataStructures;

using System.Runtime.CompilerServices;

namespace RootNav.Core.LiveWires
{
    static class LiveWireSegmentation
    {
        public static void DijkstraBetweenPoints(LiveWireGraph graph, double[,] distanceMap,  out List<Point> path, out List<int> segmentIndices, out TargetPathPoint pathIndex, Dictionary<Int32Point, int> pathPoints, params Point[] points)
        {
                // Which mode are we operating in
                bool toPathMode = pathPoints != null;

                // Initialisations
                pathIndex = new TargetPathPoint(default(Int32Point), -1);
                Int32Point currentPoint = (Int32Point)points.First();
                int width = graph.Width;
                int height = graph.Height;
                int nodeCount = width * height;
                double[] distances = new double[nodeCount];
                segmentIndices = new List<int>();

                bool[] visited = new bool[nodeCount];

                Int32Point?[][] previous = new Int32Point?[width][];
                for (int i = 0; i < width; i++)
                {
                    previous[i] = new Int32Point?[height];
                }

                DateTime startTime = DateTime.Now;

                List<Point> outputPoints = null;

                // Loop for one extra in paths mode
                int pointsLoopLength = toPathMode ? points.Length + 1 : points.Length;

                for (int pointIndex = 1; pointIndex < pointsLoopLength; pointIndex++)
                {
                    // Loop Initialisations
                    Int32Point targetPoint;// = (Int32Point)points[pointIndex];
                    bool toPaths = false;

                    if (!toPathMode || pointIndex < points.Length)
                    {
                        targetPoint = (Int32Point)points[pointIndex];
                    }
                    else
                    {
                        targetPoint = default(Int32Point);
                        toPaths = true;
                    }

                    FibonacciHeap<double, Int32Point> unvisitedheap = new FibonacciHeap<double, Int32Point>();
                    Node<double, Int32Point>[] nodeindex = new Node<double, Int32Point>[width * height];

                    for (int x = 0; x < nodeCount; x++)
                    {
                        distances[x] = double.MaxValue;
                        visited[x] = false;
                    }

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            previous[x][y] = null;
                        }
                    }

                    distances[currentPoint.Y * width + currentPoint.X] = 0;

                    Int32Point pN = new Int32Point(currentPoint.X, currentPoint.Y);
                    Node<double, Int32Point> pixelNode = new Node<double, Int32Point>(0, pN);
                    unvisitedheap.Insert(pixelNode);
                    nodeindex[currentPoint.Y * width + currentPoint.X] = pixelNode;

                    // Dijkstra's Algorithm between sourcePoint and nextPoint
                    while (unvisitedheap.Count > 0)
                    {
                        // Get next priority point u
                        Node<double, Int32Point> n = unvisitedheap.Minimum();
                        unvisitedheap.RemoveMinimum();

                        Int32Point u = n.value;

                        int uIndex = u.Y * width + u.X;

                        // if dist[u] == infinity then break
                        if (distances[uIndex] == double.MaxValue)
                            break;

                        if (!toPathMode)
                        {
                            // if u == target we are done
                            if (u.X == targetPoint.X && u.Y == targetPoint.Y)
                                break;
                        }
                        else
                        {
                            if (!toPaths)
                            {
                                if (u.X == targetPoint.X && u.Y == targetPoint.Y)
                                    break;
                            }
                            else
                            {
                                // Search hash set rather than target point
                                if (pathPoints.ContainsKey(u))
                                {
                                    pathIndex = new TargetPathPoint(u, pathPoints[u]);
                                    break;
                                }
                            }
                        }

                        // for each possible neighbour v
                        for (int i = 0; i < 8; i++)
                        {
                            double distance;
                            Int32Point? nV = GetNeighbour8(u, i, graph, out distance);

                            if (nV == null)
                                continue;

                            Int32Point v = nV.Value;
                            int vIndex = v.Y * width + v.X;

                            if (visited[vIndex])
                                continue;

                            double newDistance = distances[uIndex] + distance;
                            double remainingDistance = (0.03 * Math.Sqrt(Math.Pow(v.X - targetPoint.X, 2.0) + Math.Pow(v.Y - targetPoint.Y, 2.0))) + 2 * (Math.Pow(distanceMap[v.X, v.Y], 2));

                            if (newDistance < distances[vIndex])
                            {
                                // Adjust V and re-prioritise
                                Node<double, Int32Point> vNode = nodeindex[vIndex];
                                if (vNode == null)
                                {
                                    Int32Point p = new Int32Point(v.X, v.Y);
                                    vNode = new Node<double, Int32Point>(newDistance + remainingDistance, p);
                                    unvisitedheap.Insert(vNode);
                                    nodeindex[vIndex] = vNode;
                                    distances[vIndex] = newDistance;
                                    previous[v.X][v.Y] = u;
                                }
                                else
                                {
                                    distances[vIndex] = newDistance;
                                    previous[v.X][v.Y] = u;
                                    unvisitedheap.DecreaseKey(vNode, newDistance + remainingDistance);
                                }
                            }

                        }
                        visited[uIndex] = true;
                    }

                    // Finalisation
                    List<Point> shortestPathPoints = new List<Point>();
                    Point f = default(Point);

                    if (!toPathMode)
                    {
                        f = (Point)targetPoint;
                    }
                    else
                    {
                        if (toPaths)
                        {
                            f = (Point)pathIndex.Point;
                        }
                        else
                        {
                            f = (Point)targetPoint;
                        }
                    }

                    shortestPathPoints.Add(f);
                    int fIndex = (int)f.Y * width + (int)f.X;
                    while (previous[(int)f.X][(int)f.Y] != null)
                    {
                        shortestPathPoints.Insert(0, (Point)previous[(int)f.X][(int)f.Y]);
                        f = (Point)previous[(int)f.X][(int)f.Y];
                        fIndex = (int)f.Y * width + (int)f.X;
                    }

                    // Append to outputPoints
                    if (outputPoints == null)
                    {
                        outputPoints = shortestPathPoints;
                    }
                    else
                    {
                        segmentIndices.Add(outputPoints.Count);
                        foreach (Point p in shortestPathPoints)
                            outputPoints.Add(p);
                    }

                    // Preparation for next loop
                    if (!toPathMode)
                    {
                        currentPoint = (Int32Point)points[pointIndex];
                    }
                    else if (!toPaths)
                    {
                        currentPoint = (Int32Point)points[pointIndex];
                    }
                }

                path = outputPoints;
        }

        private static Int32Point? GetNeighbour4(Int32Point u, int neighbourIndex, LiveWireGraph graph, out double distance)
        {
            int width = graph.Width;
            int height = graph.Height;
            int x = u.X;
            int y = u.Y;
            distance = 0.0;

            switch (neighbourIndex)
            {
                case 0:
                    if (x >= width - 1)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x, 0];
                        return new Int32Point(x + 1, y);
                    }
                case 1:
                    if (y >= height -1)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x, 2];
                        return new Int32Point(x, y + 1);
                    }
                case 2:
                    if (x == 0)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x - 1, 0];
                        return new Int32Point(x - 1, y);
                    }
                case 3:
                    if (y == 0)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x - width, 2];
                        return new Int32Point(x, y - 1);
                    }
                default:
                    return null;
            }
        }

        private static Int32Point? GetNeighbour8(Int32Point u, int neighbourIndex, LiveWireGraph graph, out double distance)
        {
            int width = graph.Width;
            int height = graph.Height;
            int x = u.X;
            int y = u.Y;
            distance = 0.0;

            switch (neighbourIndex)
            {
                case 0:
                    if (x >= width - 1)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x, 0];
                        return new Int32Point(x + 1, y);
                    }
                case 1:
                    if (x >= width - 1 || y >= height - 1)
                        return null;  
                    else
                    {
                        distance = graph.EdgeData[y * width + x, 1];
                        return new Int32Point(x + 1, y + 1);
                    }
                case 2:
                    if (y >= height - 1)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x, 2];
                        return new Int32Point(x, y + 1);
                    }
                case 3:
                    if (x == 0 || y >= height - 1)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x, 3];
                        return new Int32Point(x - 1, y + 1);
                    }
                case 4:
                    if (x == 0)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x - 1, 0];
                        return new  Int32Point(x - 1, y);
                    }
                case 5:
                    if (x == 0 || y == 0)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x - width - 1, 1];
                        return new Int32Point(x - 1, y - 1);
                    }
                case 6:
                    if (y == 0)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x - width, 2];
                        return new Int32Point(x, y - 1);
                    }
                case 7:
                    if (y == 0 || x >= width - 1)
                        return null;
                    else
                    {
                        distance = graph.EdgeData[y * width + x + 1 - width, 3];
                        return new Int32Point(x + 1, y - 1);
                    }
                default:
                    return null;
            }
        }
    }

    public struct TargetPathPoint
    {
        private Int32Point point;

        public Int32Point Point
        {
            get { return point; }
        }
        private int pathIndex;

        public int ParentIndex
        {
            get { return pathIndex; }
        }

        public TargetPathPoint(Int32Point point, int pathIndex)
        {
            this.point = point;
            this.pathIndex = pathIndex;
        }

        public override bool Equals(object obj)
        {
            if ((null == obj) || !(obj is TargetPathPoint))
            {
                return false;
            }

            TargetPathPoint value = (TargetPathPoint)obj;
            return this.point.Equals(value.point);
        }

        public override int GetHashCode()
        {
            return this.point.GetHashCode();
        }
    }
}
