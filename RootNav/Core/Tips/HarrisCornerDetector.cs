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
    class HarrisCornerDetector
    {
        private double gaussianSigma = 1.0;

        public double GaussianSigma
        {
            get { return gaussianSigma; }
            set { gaussianSigma = value; }
        }

        private int lowerThreshold = 3000;

        public int LowerThreshold
        {
            get { return lowerThreshold; }
            set { lowerThreshold = value; }
        }

        private int upperThreshold = 15000;

        public int UpperThreshold
        {
            get { return upperThreshold; }
            set { upperThreshold = value; }
        }


        private double k = 0.13;

        public double K
        {
            get { return k; }
            set { k = value; }
        }

        public unsafe List<Tuple<Int32Point, double>> FindCorners(WriteableBitmap wbmp)
        {
            int width = wbmp.PixelWidth;
            int height = wbmp.PixelHeight;

            double[] ix = new double[width * height];
            double[] iy = new double[width * height];
            double[] ixy = new double[width * height];
            double[] gix = new double[width * height];
            double[] giy = new double[width * height];
            double[] gixy = new double[width * height];

            // Convert to grayscale if needed
            WriteableBitmap grayImage = wbmp;
            if (grayImage.Format != PixelFormats.Gray8)
            {
                grayImage = RootNav.Core.Imaging.ImageProcessor.MakeGreyscale8bpp(wbmp);
            }

            // Compute ix, iy and ixy derivatives
            int r = calculateKernelSize(this.GaussianSigma);
            double[,] kernel = createKernel(this.GaussianSigma, r);
            double[,] sobelX = new double[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            double[,] sobelY = new double[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            byte* grayBuffer = (byte*)grayImage.BackBuffer.ToPointer();
            int grayStride = grayImage.BackBufferStride;

            // SOBEL
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    double imageTotalX = 0.0;
                    double imageTotalY = 0.0;

                    for (int u = -1; u <= 1; u++)
                    {
                        for (int v = -1; v <= 1; v++)
                        {
                            int kX = x + u;
                            int kY = y + v;

                            imageTotalX += *(grayBuffer + (kY * grayStride) + kX) * sobelX[u + 1, v + 1];
                            imageTotalY += *(grayBuffer + (kY * grayStride) + kX) * sobelY[u + 1, v + 1];
                        }
                    }

                    double iX = imageTotalX;
                    double iY = imageTotalY;

                    ix[y * width + x] = iX * iX;
                    iy[y * width + x] = iY * iY;
                    ixy[y * width + x] = iX * iY;
                }
            }

            // Blur
            for (int x = r; x < width - r; x++)
            {
                for (int y = r; y < height - r; y++)
                {
                    double imageTotalX = 0.0;
                    double imageTotalY = 0.0;
                    double imageTotalXY = 0.0;

                    for (int u = -r; u <= r; u++)
                    {
                        for (int v = -r; v <= r; v++)
                        {
                            int kX = x + u;
                            int kY = y + v;

                            imageTotalX += ix[kY * width + kX] * kernel[u + r, v + r];
                            imageTotalY += iy[kY * width + kX] * kernel[u + r, v + r];
                            imageTotalXY += ixy[kY * width + kX] * kernel[u + r, v + r];
                        }
                    }

                    gix[y * width + x] = imageTotalX;
                    giy[y * width + x] = imageTotalY;
                    gixy[y * width + x] = imageTotalXY;
                }
            }

           // Compute H at each pixel
           double[] hMap = new double[width * height];

            for (int x = r; x < width - r; x++)
            {
                for (int y = r; y < height - r; y++)
                {
                    double A = gix[y * width + x];
                    double B = giy[y * width + x];
                    double C = gixy[y * width + x];

                    // Nobel Variation of the Harris function
                    double M = (A * B - C * C) / (A + B + 1.1920929E-07);
                    //double M = (A * B - C * C) - (k * ((A + B) * (A + B)));

                    // Threshold
                    if (M > LowerThreshold)
                    {
                        hMap[y * width + x] = M;
                    }
                }
            }

            int nMaxRadius = 1;

            // Non-maximal suppression
            List<Tuple<Int32Point, double>> outputList = new List<Tuple<Int32Point, double>>();
            for (int x = r; x < width - r; x++)
            {
                for (int y = r; y < height - r; y++)
                {
                    double currentValue = hMap[y * width + x];

                    for (int u = -nMaxRadius; u <= nMaxRadius; u++)
                    {
                        if (currentValue == 0)
                        {
                            break;
                        }

                        for (int v = -nMaxRadius; v <= nMaxRadius; v++)
                        {
                            if (hMap[(y + v) * width + x + u] > currentValue)
                            {
                                currentValue = 0;
                                break;
                            }
                        }
                    }

                    if (currentValue > 0 && currentValue < UpperThreshold)
                    {
                        outputList.Add(new Tuple<Int32Point, double>(new Int32Point(x, y), hMap[y * width + x]));
                    }
                }
            }

            return outputList;
        }

        private int calculateKernelSize(double sigma)
        {
            return Math.Min((int)Math.Round(sigma * 2.5), 25);
        }

        private double[,] createKernel(double sigma, int r)
        {
            int size = r * 2 + 1;
            double[,] kernel = new double[size,size];
            double F1 = 1.0 / (sigma * Math.Sqrt(2 * Math.PI));

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    int x = i - r;
                    int y = j - r;
                    kernel[i,j] = F1 * Math.Pow(Math.E, -(Math.Pow(x, 2.0) / (2 * Math.Pow(sigma, 2.0))));
                    kernel[i, j] *= F1 * Math.Pow(Math.E, -(Math.Pow(y, 2.0) / (2 * Math.Pow(sigma, 2.0))));
                }
            }

            double total = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    total += kernel[i, j];
                }
            }

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    kernel[i,j] /= total;
                }
            }

            return kernel;
        }

    }
}
