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
    public class LiveWireGraph
    {
        private double[,] edgeData = null;

        public double[,] EdgeData
        {
            get { return edgeData; }
            set { edgeData = value; }
        }

        public int Width { get; set; }
        public int Height { get; set; }

        public LiveWireGraph(double[,] edgeData, int width, int height)
        {
            this.edgeData = edgeData;
            this.Width = width;
            this.Height = height;
        }

        public static LiveWireGraph FromProbabilityMap(double[] probabilityMap, int width, int height)
        {
            // Root2
            double RootTwo = Math.Sqrt(2.0);

            // 0: x     ->  x+1
            // 1: x,y   ->  x+1,y+1
            // 2: y     ->  y+1
            // 3: x,y   ->  x-1,y+1

            double[,] probabilityEdges = new double[width * height,4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    // 0
                    if (x < width - 1)
                    {
                        probabilityEdges[index,0] = 1 - probabilityMap[index] * probabilityMap[index + 1];

                        // 1
                        if (y < height - 1)
                        {
                            probabilityEdges[index,1] = (1 - probabilityMap[index] * probabilityMap[index + width + 1]) * RootTwo;
                        }
                    }

                    // 2
                    if (y < height - 1)
                    {
                        probabilityEdges[index,2] = 1 - probabilityMap[index] * probabilityMap[index + width];
                        
                        // 3
                        if (x > 0)
                        {
                            probabilityEdges[index,3] = (1 - probabilityMap[index] * probabilityMap[index + width - 1]) * RootTwo;
                        }
                    }
                }
            }

            return new LiveWireGraph(probabilityEdges, width, height);
        } 
    }
}
