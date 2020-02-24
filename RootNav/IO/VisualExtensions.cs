using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;           // Need reference to System.Drawing.dll
using System.Windows;
using System.Windows.Interop;   // Need reference to PresentationCore.dll
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RootNav.IO
{

    /// <summary>
    /// Extension Methods for the System.Windows.Media.Visual Class
    /// </summary>
    public static class VisualExtensions
    {
        /// <summary>
        /// Returns the contents of a WPF Visual as a Bitmap in PNG format.
        /// </summary>
        /// <param name="visual">A WPF Visual.</param>
        /// <returns>A GDI+ System.Drawing.Bitmap.</returns>
        public static Bitmap PngBitmap(this Visual visual)
        {
            // Get height and width
            int height = (int)(double)visual.GetValue(FrameworkElement.ActualWidthProperty);
            int width = (int)(double)visual.GetValue(FrameworkElement.ActualHeightProperty);

            // Render
            RenderTargetBitmap rtb =
                new RenderTargetBitmap(
                    height,
                    width,
                    96,
                    96,
                    PixelFormats.Default);
            rtb.Render(visual);

            // Encode
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            encoder.Save(stream);

            // Create Bitmap
            Bitmap bmp = new Bitmap(stream);
            stream.Close();

            return bmp;
        }

        /// <summary>
        /// Returns the contents of a WPF Visual as a BitmapSource, e.g.
        /// for binding to an Image control.
        /// </summary>
        /// <param name="visual">A WPF Visual.</param>
        /// <returns>A set of pixels.</returns>
        public static BitmapSource BitmapSource(this Visual visual)
        {
            Bitmap bmp = visual.PngBitmap();
            IntPtr hBitmap = bmp.GetHbitmap();
            BitmapSizeOptions sizeOptions = BitmapSizeOptions.FromEmptyOptions();
            return Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                sizeOptions);
        }
    }
}
