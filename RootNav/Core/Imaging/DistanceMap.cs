using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections;
using System.Linq;
using System.Windows.Media.Imaging;

using RootNav.Core.LiveWires;

namespace RootNav.Core.Imaging
{
    static class DistanceMap
    {
        [Flags]
        public enum NeighbourCode
        {
            q1, q2, q3, q4, q5, q6, q7, q8
        }

        public static unsafe double[,] CreateDistanceMap(WriteableBitmap wbmp)
        {
            int width = wbmp.PixelWidth;
            int height = wbmp.PixelHeight;

            byte[] bPixelsInterior = new byte[width * height];
            float[] fPixelsInterior = new float[width * height];
            byte[] bPixelsExterior = new byte[width * height];
            float[] fPixelsExterior = new float[width * height];
            const int NO_POINT = -1;

            // Convert to grayscale if needed
            WriteableBitmap grayImage = wbmp;
            if (grayImage.Format != PixelFormats.Gray8)
            {
                grayImage = RootNav.Core.Imaging.ImageProcessor.MakeGreyscale8bpp(wbmp);
            }

            byte* grayBuffer = (byte*)grayImage.BackBuffer.ToPointer();
            int grayStride = grayImage.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bPixelsInterior[y * width + x] = *(grayBuffer + y * grayStride + x) > 0 ? (byte)255 : (byte)0;

                    if (bPixelsInterior[y * width + x] != 0)
                        fPixelsInterior[y * width + x] = float.MaxValue;

                    bPixelsExterior[y * width + x] = *(grayBuffer + y * grayStride + x) == 0 ? (byte)255 : (byte)0;

                    if (bPixelsExterior[y * width + x] != 0)
                        fPixelsExterior[y * width + x] = float.MaxValue;
                }
            }

            double[,] interiorDistanceMap = DistanceMapFromMasks(width, height, NO_POINT, bPixelsInterior, fPixelsInterior);
            double[,] exteriorDistanceMap = DistanceMapFromMasks(width, height, NO_POINT, bPixelsExterior, fPixelsExterior);

            double[,] combinedDistanceMap = new double[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    combinedDistanceMap[x,y] = 0.5 * interiorDistanceMap[x,y] + 0.5 * (1 - exteriorDistanceMap[x,y]);
                }
            }

            /* Testing Code
            WriteableBitmap outbmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            outbmp.Lock();
            byte* ptr = (byte*)outbmp.BackBuffer.ToPointer();
            int stride = outbmp.BackBufferStride;

            for (int y = 0; y < wbmp.PixelHeight; y++)
            {
                for (int x = 0; x < wbmp.PixelWidth; x++)
                {
                    *(ptr + y * stride + x) = (byte)(combinedDistanceMap[x, y] * 255);
                }
            }

            outbmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, wbmp.PixelWidth, wbmp.PixelHeight));
            outbmp.Unlock();

            IO.ImageEncoder.SaveImage("G:\\combinedmap.png", outbmp, IO.ImageEncoder.EncodingType.PNG);
            */

            return interiorDistanceMap;
        }

        public static unsafe double[,] DistanceMapFromMasks(int width, int height, int NO_POINT, byte[] bPixels, float[] fPixels)
        {
            int[] pointBuf0 = new int[width];  //two buffers for two passes; low short contains x, high short y
            int[] pointBuf1 = new int[width];

            // pass 1 & 2: increasing y
            for (int x = 0; x < width; x++)
            {
                pointBuf0[x] = NO_POINT;
                pointBuf1[x] = NO_POINT;
            }
            for (int y = 0; y < height; y++)
            {
                edmLine(bPixels, fPixels, pointBuf0, pointBuf1, width, y * width, y);
            }

            //pass 3 & 4: decreasing y
            for (int x = 0; x < width; x++)
            {
                pointBuf0[x] = NO_POINT;
                pointBuf1[x] = NO_POINT;
            }
            for (int y = height - 1; y >= 0; y--)
            {
                edmLine(bPixels, fPixels, pointBuf0, pointBuf1, width, y * width, y);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    fPixels[y * width + x] = (float)Math.Sqrt(fPixels[y * width + x]);
                }
            }


            float max = fPixels.Max();

            double[,] outArr = new double[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    outArr[x, y] = 1 - ((fPixels[y * width + x] / max));
                }
            }



            return outArr;
        }

        public static unsafe double[,] CreateDistanceMapOld(WriteableBitmap wbmp)
        {
            int width = wbmp.PixelWidth;
            int height = wbmp.PixelHeight;

            byte[] bPixels = new byte[width * height];
            float[] fPixels = new float[width * height];
            const int NO_POINT = -1;

            // Convert to grayscale if needed
            WriteableBitmap grayImage = wbmp;
            if (grayImage.Format != PixelFormats.Gray8)
            {
                grayImage = RootNav.Core.Imaging.ImageProcessor.MakeGreyscale8bpp(wbmp);
            }

            byte* grayBuffer = (byte*)grayImage.BackBuffer.ToPointer();
            int grayStride = grayImage.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bPixels[y * width + x] = *(grayBuffer + y * grayStride + x) > 0 ? (byte)255 : (byte)0;

                    if (bPixels[y * width + x] != 0)
                        fPixels[y * width + x] = float.MaxValue;
                }
            }

            int[] pointBuf0 = new int[width];  //two buffers for two passes; low short contains x, high short y
            int[] pointBuf1 = new int[width];

            // pass 1 & 2: increasing y
            for (int x = 0; x < width; x++)
            {
                pointBuf0[x] = NO_POINT;
                pointBuf1[x] = NO_POINT;
            }
            for (int y = 0; y < height; y++)
            {
                edmLine(bPixels, fPixels, pointBuf0, pointBuf1, width, y * width, y);
            }

            //pass 3 & 4: decreasing y
            for (int x = 0; x < width; x++)
            {
                pointBuf0[x] = NO_POINT;
                pointBuf1[x] = NO_POINT;
            }
            for (int y = height - 1; y >= 0; y--)
            {
                edmLine(bPixels, fPixels, pointBuf0, pointBuf1, width, y * width, y);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    fPixels[y * width + x] = (float)Math.Sqrt(fPixels[y * width + x]);
                }
            }


            float max = fPixels.Max();

            double[,] outArr = new double[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    outArr[x,y] = 1 - ((fPixels[y * width + x] / max));
                }
            }
           
            
            /*
            WriteableBitmap outbmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            outbmp.Lock();
            byte* ptr = (byte*)outbmp.BackBuffer.ToPointer();
            int stride = outbmp.BackBufferStride;

            Random r = new Random();

            for (int y = 0; y < wbmp.PixelHeight; y++)
            {
                for (int x = 0; x < wbmp.PixelWidth; x++)
                {
                    *(ptr + y * stride + x) = (byte)(outArr[x,y] * 255);
                }
            }

            outbmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, wbmp.PixelWidth, wbmp.PixelHeight));
            outbmp.Unlock();

            IO.ImageEncoder.SaveImage("G:\\map.png", outbmp, IO.ImageEncoder.EncodingType.PNG);
            */
            
            return outArr;
        }

        private static void edmLine(byte[] bPixels, float[] fPixels, int[] pointBuf0, int[] pointBuf1, int width, int offset, int y)
        {
            const int NO_POINT = -1;

            int[] points = pointBuf0;        // the buffer for the left-to-right pass
            int pPrev = NO_POINT;
            int pDiag = NO_POINT;               // point at (-/+1, -/+1) to current one (-1,-1 in the first pass)
            int pNextDiag;

            int distSqr = int.MaxValue;    // this value is used only if edges are not background

            for (int x = 0; x < width; x++, offset++)
            {
                pNextDiag = points[x];
                if (bPixels[offset] == 0)
                {
                    points[x] = x | y << 16;      // remember coordinates as a candidate for nearest background point
                }
                else
                {                        // foreground pixel:
                    float dist2 = minDist2(points, pPrev, pDiag, x, y, distSqr);
                    if (fPixels[offset] > dist2) fPixels[offset] = dist2;
                }
                pPrev = points[x];
                pDiag = pNextDiag;
            }
            offset--; //now points to the last pixel in the line
            points = pointBuf1;             // the buffer for the right-to-left pass. Low short contains x, high short y
            pPrev = NO_POINT;
            pDiag = NO_POINT;
            for (int x = width - 1; x >= 0; x--, offset--)
            {
                pNextDiag = points[x];
                if (bPixels[offset] == 0)
                {
                    points[x] = x | y << 16;      // remember coordinates as a candidate for nearest background point
                }
                else
                {                        // foreground pixel:
                    float dist2 = minDist2(points, pPrev, pDiag, x, y, distSqr);
                    if (fPixels[offset] > dist2) fPixels[offset] = dist2;
                }
                pPrev = points[x];
                pDiag = pNextDiag;

            }
        } //private void edmLine


        private static float minDist2(int[] points, int pPrev, int pDiag, int x, int y, int distSqr)
        {
            const int NO_POINT = -1;
            int p0 = points[x];              // the nearest background point for the same x in the previous line
            int nearestPoint = p0;
            if (p0 != NO_POINT)
            {
                int x0 = p0 & 0xffff; int y0 = (p0 >> 16) & 0xffff;
                int dist1Sqr = (x - x0) * (x - x0) + (y - y0) * (y - y0);
                if (dist1Sqr < distSqr)
                    distSqr = dist1Sqr;
            }
            if (pDiag != p0 && pDiag != NO_POINT)
            {
                int x1 = pDiag & 0xffff; int y1 = (pDiag >> 16) & 0xffff;
                int dist1Sqr = (x - x1) * (x - x1) + (y - y1) * (y - y1);
                if (dist1Sqr < distSqr)
                {
                    nearestPoint = pDiag;
                    distSqr = dist1Sqr;
                }
            }
            if (pPrev != pDiag && pPrev != NO_POINT)
            {
                int x1 = pPrev & 0xffff; int y1 = (pPrev >> 16) & 0xffff;
                int dist1Sqr = (x - x1) * (x - x1) + (y - y1) * (y - y1);
                if (dist1Sqr < distSqr)
                {
                    nearestPoint = pPrev;
                    distSqr = dist1Sqr;
                }
            }
            points[x] = nearestPoint;
            return (float)distSqr;
        }
    }
}
