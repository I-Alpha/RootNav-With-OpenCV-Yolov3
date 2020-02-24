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
    static class LiveWireGraphResolution
    {
        public static LiveWireGraph FromProbabilityMap(double[] probabilityMap, int width, int height, double resolution)
        {
            // 1 / Root2
            double RootTwo = Math.Sqrt(2.0);

            // 0: x     ->  x+1 (E)
            // 1: x,y   ->  x+1,y+1 (SE)
            // 2: y     ->  y+1 (S)
            // 3: x,y   ->  x-1,y+1 (SW)

            int graphWidth = (int)(width * resolution);
            int graphHeight = (int)(height * resolution);

            double[,] probabilityEdges = new double[graphWidth * graphHeight, 4];

            for (int y = 0; y < graphHeight; y++)
            {
                for (int x = 0; x < graphWidth; x++)
                {
                    // Calculate image position
                    int edgesIndex = y * graphWidth + x;
                    int probabilityMapIndex = (int)(y / resolution) * width + (int)(x / resolution);

                    // 0
                    if (x < graphWidth - 1)
                    {
                        //probabilityEdges[x, y] = 1 - probabilityMap[probabilityMapIndex] * probabilityMap[probabilityMapIndex + 1];
                        probabilityEdges[edgesIndex, 0] = 1 - CalculateProbabilityBetweenPoints(probabilityMap, width, height, (int)(x / resolution), (int)(y / resolution), (int)((x + 1) / resolution), (int)((y) / resolution));

                        // 1
                        if (y < graphHeight - 1)
                        {
                            //probabilityEdges[x, y] = 1 - probabilityMap[probabilityMapIndex] * probabilityMap[probabilityMapIndex + width + 1] * OneOverRootTwo;
                            probabilityEdges[edgesIndex, 1] = (1 - CalculateProbabilityBetweenPoints(probabilityMap, width, height, (int)(x / resolution), (int)(y / resolution), (int)((x + 1) / resolution), (int)((y + 1) / resolution))) * RootTwo;
                        }
                    }

                    // 2
                    if (y < graphHeight - 1)
                    {
                        //probabilityEdges[x, y] = 1 - probabilityMap[probabilityMapIndex] * probabilityMap[probabilityMapIndex + width];
                        probabilityEdges[edgesIndex, 2] = 1 - CalculateProbabilityBetweenPoints(probabilityMap, width, height, (int)(x / resolution), (int)(y / resolution), (int)((x) / resolution), (int)((y + 1) / resolution));
                        
                        // 3
                        if (x > 0)
                        {
                            //probabilityEdges[x, y] = 1 - probabilityMap[probabilityMapIndex] * probabilityMap[probabilityMapIndex + width - 1] * OneOverRootTwo;
                            probabilityEdges[edgesIndex, 3] = (1 - CalculateProbabilityBetweenPoints(probabilityMap, width, height, (int)(x / resolution), (int)(y / resolution), (int)((x - 1) / resolution), (int)((y + 1) / resolution))) * RootTwo;
                        }
                    }
                }
            }

            /*
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(graphWidth, graphHeight);

            for (int y = 0; y < graphHeight; y++)
            {
                for (int x = 0; x < graphWidth; x++)
                {
                    int index = y * graphWidth + x;
                    byte p = (byte)(probabilityEdges[index,3] * 255);
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(p, p, p));
                }
            }

            bmp.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\edges.png");
            */

            return new LiveWireGraph(probabilityEdges, graphWidth, graphHeight);
        }

        private static double CalculateProbabilityBetweenPoints(double[] probabilityMap, int width, int height, int x1, int y1, int x2, int y2)
        {
            // Use bresenham's line drawing algorithm to obtain all pixels along the line
            double dx = Math.Abs(x2 - x1);
            double dy = Math.Abs(y2 - y1);
            
            // TESTING
            // return probabilityMap[y1 * width + x1] * probabilityMap[y2 * width + x2];
            
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            double err = dx - dy; 

            int x = x1,
                y = y1,
                fx = x2,
                fy = y2;

            double totalProbability = 1.0;

            while (true)
            {
                totalProbability *= probabilityMap[y * width + x];

                if (x == fx && y == fy)
                    break;

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
            return totalProbability;
        }

    }
}
