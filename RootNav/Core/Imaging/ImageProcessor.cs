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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RootNav.Core.Imaging
{
    /// <summary>
    /// Static class providing some image processing algorithms.
    /// </summary>
    static class ImageProcessor
    {
        /// <summary>
        /// Creates a Difference of Gaussian operator
        /// </summary>
        /// <param name="radius">The radius of the kernel to be produced. For best results should be 2 * sigma in size at least.</param>
        /// <param name="sigma1">The standard deviation of the first gaussian.</param>
        /// <param name="K">The ratio between the supplied sigma and the s.d. of the second gaussian.</param>
        /// <param name="scale1">Optional scaling parameter for the first gaussian.</param>
        /// <param name="scale2">Optional scaling parameter for the second gaussian.</param>
        /// <returns></returns>
        private static double[,] CreateDoGOperator(int radius, double sigma1, double K, double scale1 = 1.0, double scale2 = 1.0)
        {
            int dimension = radius * 2 + 1;
            double[,] matrix = new double[dimension, dimension];

            double TwoSigma2 = 2 * Math.Pow(sigma1, 2.0);
            double TwoK2Sigma2 = 2 * Math.Pow(K, 2.0) * Math.Pow(sigma1, 2.0);

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    double G1 = 1 / (TwoSigma2 * Math.PI);
                    G1 *= Math.Pow(Math.E, -(Math.Pow(x, 2) + Math.Pow(y, 2)) / TwoSigma2);

                    double G2 = 1 / (TwoK2Sigma2 * Math.PI);
                    G2 *= Math.Pow(Math.E, -(Math.Pow(x, 2) + Math.Pow(y, 2)) / TwoK2Sigma2);

                    matrix[x + radius, y + radius] = scale1 * G1 - scale2 * G2;
                }
            }
            return matrix;
        }

        unsafe private static byte GetPixel(WriteableBitmap image, uint mask, int shift, int x, int y)
        {
            uint* writeableBitmapBuffer = (uint*)image.BackBuffer.ToPointer();
            int offset = y * (image.BackBufferStride / 4) + x;
            uint intensity = mask & *(writeableBitmapBuffer + offset);
            intensity = intensity >> shift;
            return (byte)intensity;
        }

        /// <summary>
        /// Applies a sobel filter to the provided image, and returns the result
        /// </summary>
        /// <param name="source">The source image to be filtered</param>
        /// <returns>A new WriteableBitmap containing the output of the sobel operator</returns>
        unsafe public static WriteableBitmap SobelFilter(WriteableBitmap source)
        {
            if (source.Format.BitsPerPixel != 32 || source.Format.Masks.Count != 3)
                throw new Exception("Writeable Bitmap must be Bgr32 format");

            WriteableBitmap destination = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Bgr32, null);

            uint* writeableBitmapBuffer = (uint*)source.BackBuffer.ToPointer();
            uint* writeableBitmapBufferRow1 = (uint*)source.BackBuffer.ToPointer();
            uint* writeableBitmapBufferRow2 = (uint*)source.BackBuffer.ToPointer();
            uint* writeableBitmapBufferRow3 = (uint*)source.BackBuffer.ToPointer();
            uint* writeableBitmapBuffer2 = (uint*)destination.BackBuffer.ToPointer();

            int[,] ymask = new int[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
            int[,] xmask = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };

            // Image parameters
            uint mask = 0x000000FF;

            int width = source.PixelWidth;
            int height = source.PixelHeight;

            int stride = source.BackBufferStride / 4;

            destination.Lock();

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    writeableBitmapBufferRow1 = writeableBitmapBuffer + ((y - 1) * stride) + x - 1;
                    writeableBitmapBufferRow2 = writeableBitmapBuffer + (y * stride) + x - 1;
                    writeableBitmapBufferRow3 = writeableBitmapBuffer + ((y + 1) * stride) + x - 1;

                    double xtotal = ((mask & *writeableBitmapBufferRow1) * -1) + ((mask & *writeableBitmapBufferRow2++) * -2) + ((mask & *writeableBitmapBufferRow3) * -1);
                    double ytotal = ((mask & *writeableBitmapBufferRow1++) * -1) + ((mask & *writeableBitmapBufferRow3++) * 1);

                    ytotal += ((mask & *writeableBitmapBufferRow1++) * -2)  + ((mask & *writeableBitmapBufferRow3++) * 2);
                    writeableBitmapBufferRow2++;

                    xtotal += ((mask & *writeableBitmapBufferRow1) * 1) + ((mask & *writeableBitmapBufferRow2) * 2) + ((mask & *writeableBitmapBufferRow3) * 1);
                    ytotal += ((mask & *writeableBitmapBufferRow1) * -1) + ((mask & *writeableBitmapBufferRow3) * 1);

                    double g = Math.Sqrt(Math.Pow(xtotal, 2) + Math.Pow(ytotal, 2));

                    // Scale 0-255
                    g /= 1150;
                    g *= 255;
                    uint intensity = (byte)Math.Min(255, g);

                    uint bgr32 = 0 | intensity | intensity << 8 | intensity << 16;

                    *(writeableBitmapBuffer2 + (y * stride) + x) = bgr32;
                }
            }
            destination.AddDirtyRect(new Int32Rect(0, 0, destination.PixelWidth, destination.PixelHeight));
            destination.Unlock();
            return destination;
        }

        /// <summary>
        /// Applies a greyscale conversion to the provided image, and returns the result
        /// </summary>
        /// <param name="source">The source image to be converted</param>
        /// <returns>A new WriteableBitmap containing a greyscale image converted from the source</returns>
        unsafe public static WriteableBitmap MakeGreyscale32bpp(WriteableBitmap source)
        {
            if (source.Format.BitsPerPixel != 32 || source.Format.Masks.Count != 3)
                throw new Exception("Writeable Bitmap must be Bgr32 format");

            WriteableBitmap destination = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Bgr32, null);

            uint* writeableBitmapBufferSrc = (uint*)source.BackBuffer.ToPointer();
            uint* writeableBitmapBufferDst = (uint*)destination.BackBuffer.ToPointer();

            // Image parameters
            int width = source.PixelWidth;
            int height = source.PixelHeight;

            int stride = source.BackBufferStride / 4;

            uint rmask = 0x00FF0000;
            uint gmask = 0x0000FF00;
            uint bmask = 0x000000FF;


            destination.Lock();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    uint rgb = *(writeableBitmapBufferSrc + (y * stride) + x);

                    uint intensity = (uint)(0.30 * ((rmask & rgb) >> 16))
                                   + (uint)(0.59 * ((gmask & rgb) >> 8))
                                   + (uint)(0.11 * ((bmask & rgb)));

                    uint bgr32 = 0 | intensity | intensity << 8 | intensity << 16;

                    *(writeableBitmapBufferDst + (y * stride) + x) = bgr32;
                }
            }
            destination.AddDirtyRect(new Int32Rect(0, 0, destination.PixelWidth, destination.PixelHeight));
            destination.Unlock();
            return destination;
        }

        /// <summary>
        /// Applies a greyscale conversion to the provided image, and returns the result
        /// </summary>
        /// <param name="source">The source image to be converted</param>
        /// <returns>A new WriteableBitmap containing a greyscale image converted from the source</returns>
        unsafe public static WriteableBitmap MakeGreyscale8bpp(WriteableBitmap source)
        {
            if (source.Format.BitsPerPixel != 32 || source.Format.Masks.Count != 3)
                throw new Exception("Writeable Bitmap must be Bgr32 format");

            WriteableBitmap destination = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Gray8, null);

            uint* writeableBitmapBufferSrc = (uint*)source.BackBuffer.ToPointer();
            byte* writeableBitmapBufferDst = (byte*)destination.BackBuffer.ToPointer();

            // Image parameters
            int width = source.PixelWidth;
            int height = source.PixelHeight;

            int stride = source.BackBufferStride / 4;
            int dstStride = destination.BackBufferStride;

            uint rmask = 0x00FF0000;
            uint gmask = 0x0000FF00;
            uint bmask = 0x000000FF;

            destination.Lock();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    uint rgb = *(writeableBitmapBufferSrc + (y * stride) + x);

                    uint intensity = (uint)(0.30 * ((rmask & rgb) >> 16))
                                   + (uint)(0.59 * ((gmask & rgb) >> 8))
                                   + (uint)(0.11 * ((bmask & rgb)));

                    *(writeableBitmapBufferDst + (y * dstStride) + x) = (byte)intensity;
                }
            }
            destination.AddDirtyRect(new Int32Rect(0, 0, destination.PixelWidth, destination.PixelHeight));
            destination.Unlock();
            return destination;
        }

    }
}
