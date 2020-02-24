using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace RootNav.IO
{
    public class ImageConverter
    {
        /// <summary>
        /// Converts a WriteableBitmap into BGR32 format - Currently supports 8bit indexed and BGRA32
        /// </summary>
        /// <param name="source">The source image to be converted</param>
        /// <returns>A new writeablebitmap copied from the source image in BGR32 format</returns>
        unsafe public static WriteableBitmap ConvertTo32bpp(WriteableBitmap source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            WriteableBitmap output = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, PixelFormats.Bgr32, null);
            if (source.Format == PixelFormats.Gray8)
            {
                output.Lock();

                byte* sourcePointer = (byte*)source.BackBuffer.ToPointer();
                int* outputPointer = (int*)output.BackBuffer.ToPointer();
                int sourceStride = source.BackBufferStride;
                int outputStride = output.BackBufferStride / 4;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceOffset = y * sourceStride + x;
                        int outputOffset = y * outputStride + x;

                        byte intensity = *(sourcePointer + sourceOffset);

                        int bgr32 = 0x00 | intensity | intensity << 8 | intensity << 16;
                        *(outputPointer + outputOffset) = bgr32;
                    }
                }

                output.AddDirtyRect(new Int32Rect(0, 0, output.PixelWidth, output.PixelHeight));
                output.Unlock();
            }
            if (source.Format == PixelFormats.Indexed8)
            {
                output.Lock();

                byte* sourcePointer = (byte*)source.BackBuffer.ToPointer();
                int* outputPointer = (int*)output.BackBuffer.ToPointer();
                int sourceStride = source.BackBufferStride;
                int outputStride = output.BackBufferStride / 4;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceOffset = y * sourceStride + x;
                        int outputOffset = y * outputStride + x;

                        Color sourceColour = source.Palette.Colors[*(sourcePointer + sourceOffset)];

                        int bgr32 = 0x00 | sourceColour.B | sourceColour.G << 8 | sourceColour.R << 16;
                        *(outputPointer + outputOffset) = bgr32;
                    }
                }

                output.AddDirtyRect(new Int32Rect(0, 0, output.PixelWidth, output.PixelHeight));
                output.Unlock();
            }
            else if (source.Format == PixelFormats.Bgra32)
            {
                output.Lock();
                int* sourcePointer = (int*)source.BackBuffer.ToPointer();
                int* outputPointer = (int*)output.BackBuffer.ToPointer();
                int sourceStride = source.BackBufferStride / 4;
                int outputStride = output.BackBufferStride / 4;

                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        *(outputPointer + (y * outputStride + x)) = *(sourcePointer + (y * sourceStride + x));

                output.AddDirtyRect(new Int32Rect(0, 0, output.PixelWidth, output.PixelHeight));
                output.Unlock();
            }

            return output;
        }

        /// <summary>
        /// Converts a writeable bitmap object into an image buffer. If a large amount of processing is required, this one off step will speed up analysis. This function assumes that the R, G and B values for each pixel are the same, i.e. the image is greyscale
        /// </summary>
        /// <param name="source">The writeablebitmap object to be converted</param>
        /// <returns>A byte array containing the reformatted image data</returns>
        unsafe public static byte[] WriteableBitmapToIntensityBuffer(WriteableBitmap source)
        {
            if (source.Format.BitsPerPixel != 32 || source.Format.Masks.Count != 3)
                throw new ArgumentException("Writeable Bitmap must be Bgr32 format");

            uint* writeableBitmapBuffer = (uint*)source.BackBuffer.ToPointer();
            uint mask = 0x000000FF;

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int offset = 0;
            int stride = source.BackBufferStride / 4;

            byte[] buffer = new byte[source.PixelWidth * source.PixelHeight];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    offset = y * stride + x;

                    uint val = *(writeableBitmapBuffer + offset);

                    buffer[y * width + x] = (byte)(mask & val);
                }
            }
            return buffer;
        }

        unsafe public static WriteableBitmap IntensityBufferToWriteableBitmap(byte[] buffer, int width, int height)
        {
            WriteableBitmap output = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgr32, null);

            output.Lock();
            uint* outputPointer = (uint*)output.BackBuffer.ToPointer();
            int outputStride = output.BackBufferStride / 4;

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    byte intensity = buffer[y * width + x];
                    uint bgr32 = (uint)(0 | intensity | intensity << 8 | intensity << 16);
                    *(outputPointer + (y * outputStride) + x) = bgr32;
                }

            output.AddDirtyRect(new Int32Rect(0, 0, output.PixelWidth, output.PixelHeight));
            output.Unlock();


            return output;
        }

        unsafe public static WriteableBitmap ConvertToGrayScaleUniform(WriteableBitmap source)
        {
            WriteableBitmap src = source;
            WriteableBitmap dst = new WriteableBitmap(src.PixelWidth, src.PixelHeight, 96.0, 96.0, PixelFormats.Gray8, null);

            dst.Lock();

            uint* srcBuffer = (uint*)src.BackBuffer.ToPointer();
            byte* dstBuffer = (byte*)dst.BackBuffer.ToPointer();

            int width = src.PixelWidth, height = src.PixelHeight;

            int srcOffset = 0, dstOffset = 0;
            int srcStride = src.BackBufferStride / 4;
            int dstStride = dst.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                  
                    srcOffset = y * srcStride + x;
                    dstOffset = y * dstStride + x;

                    uint argb32 = *(srcBuffer + srcOffset);
                    uint red = (argb32 & 0x00FF0000) >> 16;
                    uint grn = (argb32 & 0x0000FF00) >> 8;
                    uint blu = (argb32 & 0x000000FF);

                    byte grey = (byte)(red * 0.33 + grn * 0.34 + blu * 0.33);

                    *(dstBuffer + dstOffset) = grey;
                }
            }
            
            dst.AddDirtyRect(new Int32Rect(0, 0, dst.PixelWidth, dst.PixelHeight));
            dst.Unlock();
            if (dst.CanFreeze)
                dst.Freeze();
            dst.Freeze();
            return dst;
        }

        unsafe public static WriteableBitmap BlueChannelOnly(WriteableBitmap source)
        {
            WriteableBitmap src = source;
            WriteableBitmap dst = new WriteableBitmap(src.PixelWidth, src.PixelHeight, 96.0, 96.0, PixelFormats.Gray8, null);

            dst.Lock();

            uint* srcBuffer = (uint*)src.BackBuffer.ToPointer();
            byte* dstBuffer = (byte*)dst.BackBuffer.ToPointer();

            int width = src.PixelWidth, height = src.PixelHeight;

            int srcOffset = 0, dstOffset = 0;
            int srcStride = src.BackBufferStride / 4;
            int dstStride = dst.BackBufferStride;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {

                    srcOffset = y * srcStride + x;
                    dstOffset = y * dstStride + x;

                    uint argb32 = *(srcBuffer + srcOffset);
                    uint red = (argb32 & 0x00FF0000) >> 16;
                    uint grn = (argb32 & 0x0000FF00) >> 8;
                    uint blu = (argb32 & 0x000000FF);

                    byte grey = (byte)blu;

                    *(dstBuffer + dstOffset) = grey;
                }
            }

            dst.AddDirtyRect(new Int32Rect(0, 0, dst.PixelWidth, dst.PixelHeight));
            dst.Unlock();
            if (dst.CanFreeze)
                dst.Freeze();
            dst.Freeze();
            return dst;
        }

        public unsafe static WriteableBitmap Resize8bpp(WriteableBitmap image, int newWidth, int newHeight)
        {
            if (image.Format != PixelFormats.Gray8)
            {
                throw new InvalidOperationException("This function can only resize 8bb gray images.");
            }

            WriteableBitmap src = image;
            WriteableBitmap dst = null;

            if (src.Format == PixelFormats.Gray8)
            {
                dst = new WriteableBitmap(newWidth, newHeight, 96.0, 96.0, PixelFormats.Gray8, null);
                dst.Lock();

                byte* srcBuffer = (byte*)src.BackBuffer.ToPointer();
                byte* dstBuffer = (byte*)dst.BackBuffer.ToPointer();

                int width = src.PixelWidth;
                int height = src.PixelHeight;

                int srcStride = src.BackBufferStride;
                int dstStride = dst.BackBufferStride;

                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        double existingX = ((x / (double)newWidth) * (width - 1));
                        double xOffset = existingX - (int)existingX;
                        double existingY = ((y / (double)newHeight) * (height - 1));
                        double yOffset = existingY - (int)existingY;

                        byte i0 = *(srcBuffer + (int)(existingY) * srcStride + (int)(existingX));
                        byte i1 = *(srcBuffer + (int)(existingY) * srcStride + (int)(existingX + 1));
                        byte i2 = *(srcBuffer + (int)(existingY + 1) * srcStride + (int)(existingX));
                        byte i3 = *(srcBuffer + (int)(existingY + 1) * srcStride + (int)(existingX + 1));

                        // Pass 1
                        double i01 = (1 - xOffset) * i0 + xOffset * i1;
                        double i23 = (1 - xOffset) * i2 + xOffset * i3;

                        double i0123 = (1 - yOffset) * i01 + yOffset * i23;
                        byte intensity = (byte)i0123;

                        *(dstBuffer + y * dstStride + x) = intensity;
                    }
                }
                dst.AddDirtyRect(new Int32Rect(0, 0, newWidth, newHeight));
                dst.Unlock();
            }

            return dst;
        }



    }
}
