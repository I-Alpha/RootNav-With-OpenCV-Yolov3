using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections;
using System.Linq;
using System.Windows.Media.Imaging;

using RootNav.Core.LiveWires;

namespace RootNav.Core.Tips
{
    class TipFeatures
    {
        private int featureRadius = 16;

        public int FeatureRadius
        {
            get { return featureRadius; }
            set { featureRadius = value; }
        }

        private int minimumRootWidth = 2;

        public int MinimumRootWidth
        {
            get { return minimumRootWidth; }
            set { minimumRootWidth = value; }
        }

        private int maximumRootWidth = 12;

        public int MaximumRootWidth
        {
            get { return maximumRootWidth; }
            set { maximumRootWidth = value; }
        }

        public unsafe List<Tuple<Int32Point, double>> MatchFeatures(List<Tuple<Int32Point, double>> sourcePoints, WriteableBitmap source)
        {
            // Convert bitmap to thresholded image - at this point thresholding is adequate and computationally more efficient
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            byte[,] thresholdedArray = new byte[width, height];

            byte* srcBuffer = (byte*)source.BackBuffer.ToPointer();
            int stride = source.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    thresholdedArray[x, y] = *(srcBuffer + y * stride + x) > 128 ? (byte)1 : (byte)0;
                }
            }

            List<Tuple<Int32Point, double>> outputPoints = new List<Tuple<Int32Point, double>>();
            foreach (Tuple<Int32Point, double> kvp in sourcePoints)
            {
                Int32Point currentPoint = kvp.Item1;

                List<byte> profile = GetCircularProfile(thresholdedArray, currentPoint.X, currentPoint.Y);
                int noiseCount = 0;
                int count = CountProfileIntersections(profile, out noiseCount);

                if (count == 1 && noiseCount == 0)
                {
                    outputPoints.Add(kvp);
                }
            }

            return outputPoints;
        }

        private int CountProfileIntersections(List<byte> profile, out int noiseCount, int minimumSize = 3)
        {
            int rootCount = 0;
            noiseCount = 0;
            bool onRootPixels = false;
            int rootSizeCount = 0;

            // Cyclic considerations
            bool onStartPixel = true;
            bool isStartARootPixel = false;
            bool onStartRoot = false;
            int startCount = 0;

            foreach (byte b in profile)
            {
                if (onStartPixel)
                {
                    onStartPixel = false;
                    if (b == 1)
                    {
                        onStartRoot = true;
                        isStartARootPixel = true;
                    }
                }

                if (onRootPixels)
                {
                    if (b == 1)
                    {
                        // Still on a root
                        rootSizeCount++;
                    }
                    else if (b == 0)
                    {
                        // Leaving a root
                        if (onStartRoot)
                        {
                            startCount = rootSizeCount;
                            onStartRoot = false;
                        }

                        if (rootSizeCount >= minimumRootWidth && rootSizeCount < maximumRootWidth)
                        {
                            rootCount++;
                        }
                        else if (rootSizeCount < minimumRootWidth)
                        {
                            noiseCount++;
                        }
                       
                        onRootPixels = false;
                        rootSizeCount = 0;
                    }
                }
                else
                {
                    if (b == 1)
                    {
                        // Moving onto a root
                        onRootPixels = true;
                        rootSizeCount = 1;
                    }
                }
            }

            // Check for cycle and final root
            if (onRootPixels)
            {
                if (isStartARootPixel)
                {
                    int cyclicRootCount = rootSizeCount + startCount;

                    // The cyclic root is the correct size for a root
                    if (cyclicRootCount >= minimumRootWidth && cyclicRootCount < maximumRootWidth)
                    {
                        // Ensure that we haven't already counted this root
                        if (startCount < minimumRootWidth)
                        {
                            rootCount++;
                        }
                    }
                    else if (cyclicRootCount < minimumRootWidth)
                    {
                        noiseCount++;
                    }
                }
                else
                {
                    if (rootSizeCount >= minimumRootWidth && rootSizeCount < maximumRootWidth)
                    {
                        rootCount++;
                    }
                }
            }

            return rootCount;
        }

        private List<byte> GetCircularProfile(byte[,] map, int cX, int cY)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            List<byte>[] octants = new List<byte>[8];
            for (int i = 0; i < 8; i++)
            {
                octants[i] = new List<byte>();
            }

            List<Point>[] octantPoints = new List<Point>[8];
            for (int i = 0; i < 8; i++)
            {
                octantPoints[i] = new List<Point>();
            }

            int xBound = width - 1;
            int yBound = height - 1;
            int xx, xmx, yx, ymx, xy, xmy, yy, ymy;

            int f = 1 - featureRadius;
            int dx = 1;
            int dy = -2 * featureRadius;
            int x = 0;
            int y = featureRadius;
 
            while (x < y)
            {
                if (f >= 0)
                {
                    y--;
                    dy += 2;
                    f += dy;
                }
                x++;
                dx += 2;
                f += dx;

                // Precompute bounds for efficiency
                // X +-
                xx = Math.Min(xBound, cX + x);
                xmx = Math.Max(0, cX - x);
                xy = Math.Min(xBound, cX + y);
                xmy = Math.Max(0, cX - y);
                // Y +-
                yx = Math.Min(yBound, cY + x);
                ymx = Math.Max(0, cY - x);
                yy = Math.Min(yBound, cY + y);
                ymy = Math.Max(0, cY - y);


                // Octants are labled in ascending order, travelling clockwise from (+x,-y) - upwards
                octants[0].Add(map[xx, ymy]);
                octants[1].Insert(0, map[xy, ymx]);
                octants[2].Add(map[xy, yx]);
                octants[3].Insert(0, map[xx, yy]);
                octants[4].Add(map[xmx, yy]);
                octants[5].Insert(0, map[xmy, yx]);
                octants[6].Add(map[xmy, ymx]);
                octants[7].Insert(0, map[xmx, ymy]);
            }

            // Insert missing values into octants
            octants[0].Insert(0, map[cX, Math.Max(0, cY - featureRadius)]);
            octants[2].Insert(0, map[Math.Min(xBound, cX + featureRadius), cY]);
            octants[4].Insert(0, map[cX, Math.Min(yBound, cY + featureRadius)]);
            octants[6].Insert(0, map[Math.Max(0, cX - featureRadius), cY]);

            // Remove extra values from octants
            octants[1].RemoveAt(0);
            octants[3].RemoveAt(0);
            octants[5].RemoveAt(0);
            octants[7].RemoveAt(0);

            // Finalise
            List<byte> final = new List<byte>();
            final.AddRange(octants[0]);         
            final.AddRange(octants[1]);          
            final.AddRange(octants[2]);
            final.AddRange(octants[3]);
            final.AddRange(octants[4]);
            final.AddRange(octants[5]);
            final.AddRange(octants[6]);
            final.AddRange(octants[7]);

            return final;
        }

    }
}
