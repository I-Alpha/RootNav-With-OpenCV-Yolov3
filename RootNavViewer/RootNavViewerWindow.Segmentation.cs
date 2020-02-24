using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RootNav.Data;

namespace RootNav.Viewer
{
    /// <summary>
    /// Interaction logic for RootNavViewerWindow.xaml
    /// </summary>
    public partial class RootNavViewerWindow : Window
    {


        unsafe private void segmentedImagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = "G:\\Training Sets\\new"
            };
            var result = fbd.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            foreach (double penWidth in new double[] { 8 })
            {
                bool useCrop = true;
                bool tipsOnly = false;
                bool primaryOnly = false;
                bool lateralOnly = false;
                bool conflictOnly = false;
                bool sourceOnly = false;
                bool outputOriginalImages = false;
                //double penWidth = 10;

                this.SnapsToDevicePixels = true;
                RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

                string[] supportedDatasets = new string[] { "RSDH00", "RSDH01", "RSDH02", "RSDH03", "RSDH04", "RSDH06", "RSDH07", "RSDH08", "RSDH09", "RSDH10" };

                List<string> tags = rootReader.FilterTags(supportedDatasets, true);

                Random rand = new Random();

                string path = fbd.SelectedPath;
                string segPath = path + "\\segmentation" + penWidth;
                string imagePath = path + "\\images";
                int runningTotal = 0;
                int count = 0;
                if (!Directory.Exists(imagePath)) Directory.CreateDirectory(imagePath);
                if (!Directory.Exists(segPath)) Directory.CreateDirectory(segPath);

                foreach (string tag in tags)
                {
                    var data = rootReader.Read(tag, true);

                    // If no image, skip
                    if (data.Item3 == null) continue;

                    SceneMetadata metadata = data.Item1;
                    SceneInfo scene = data.Item2;
                    byte[] imageData = data.Item3;

                    BitmapSource currentImage;

                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        JpegBitmapDecoder decoder = new JpegBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        // Due to what appears to be a bug in .NET, wrapping in a WBMP causes a cache refresh and fixes some image corruption
                        currentImage = new WriteableBitmap(decoder.Frames[0]);
                    }

                    SolidColorBrush scb = new SolidColorBrush(Colors.White);
                    Pen p = new Pen(Brushes.White, penWidth);
                    p.StartLineCap = PenLineCap.Round;
                    p.EndLineCap = PenLineCap.Round;

                    // Obtain all root information
                    Dictionary<String, RootInfo> plantComponents = new Dictionary<string, RootInfo>();

                    foreach (PlantInfo plant in scene.Plants)
                    {
                        foreach (RootInfo root in plant)
                        {
                            plantComponents.Add(root.RelativeID, root);
                        }
                    }

                    // Initialise conflict data
                    int conflictScale = 30;
                    int conflictWidth = currentImage.PixelWidth / conflictScale;
                    int conflictHeight = currentImage.PixelHeight / conflictScale;
                    int[] rootCount = new int[conflictWidth * conflictHeight];
                    bool[] rootCounted = new bool[conflictWidth * conflictHeight];
                    
                    // Crop
                    // Find global plant bounding box
                    double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;

                    foreach (var kvp in plantComponents)
                    {
                        SampledSpline spline = kvp.Value.Spline as SampledSpline;
                        if (spline == null)
                        {
                            continue;
                        }

                        Rect r = spline.BoundingBox;

                        if (r.Left < left)
                        {
                            left = r.Left;
                        }

                        if (r.Right > right)
                        {
                            right = r.Right;
                        }

                        if (r.Top < top)
                        {
                            top = r.Top;
                        }

                        if (r.Bottom > bottom)
                        {
                            bottom = r.Bottom;
                        }

                        // If detecting conflict
                        if (conflictOnly)
                        {
                            RootInfo parent = plantComponents.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponents[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;

                            if (parent == null)
                            {

                                Array.Clear(rootCounted, 0, rootCounted.Length);
                                foreach (Point splinePoint in spline.SampledPoints)
                                {
                                    int conflictX = (int)(splinePoint.X / conflictScale);
                                    int conflictY = (int)(splinePoint.Y / conflictScale);
                                    int conflictIndex = conflictY * conflictWidth + conflictX;

                                    if (!rootCounted[conflictIndex])
                                    {
                                        rootCount[conflictIndex]++;
                                        rootCounted[conflictIndex] = true;
                                    }
                                }
                            }

                        }
                    }

                    // Render to drawingVisual, then image
                    int width = currentImage.PixelWidth;
                    int height = currentImage.PixelHeight;

                    // Optional Crop
                    Int32Rect? cropRectangle = null;
                    if (useCrop)
                    {
                        int cropPadding = 64;

                        int l = Math.Max(0, (int)(left - cropPadding));
                        int r = Math.Min(width, (int)(right + cropPadding));
                        int t = Math.Max(0, (int)(top - cropPadding));
                        int b = Math.Min(height, (int)(bottom + cropPadding));

                        cropRectangle = new Int32Rect(l, t, r - l, b - t);
                    }

                    RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                    DrawingVisual drawingVisual = new DrawingVisual();
                    DrawingContext drawingContext = drawingVisual.RenderOpen();

                    SolidColorBrush background = new SolidColorBrush(Colors.Black);
                    drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, width, height));

                    // Draw splines
                    StreamGeometry geometry = new StreamGeometry();

                    using (StreamGeometryContext sgc = geometry.Open())
                    {
                        foreach (var kvp in plantComponents)
                        {
                            SampledSpline spline = kvp.Value.Spline as SampledSpline;
                           
                            if (spline != null)
                            {
                                Point[] points = spline.SampledPoints;
                                // Optional line from start position on parent

                                if (sourceOnly)
                                {
                                    // First primary source
                                    if (kvp.Value.RelativeID == "1.1")
                                    {
                                        sgc.BeginFigure(points[0], false, false);
                                        sgc.LineTo(points[0], true, true);
                                        break;
                                    }
                                }

                                if (tipsOnly)
                                {
                                    
                                    if (primaryOnly)
                                    {
                                        RootInfo parent = plantComponents.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponents[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;
                                        if (parent == null)
                                        {
                                            sgc.BeginFigure(points[points.Length - 2], false, false);
                                            sgc.LineTo(points[points.Length - 1], true, true);
                                        }
                                    }
                                    else if (lateralOnly)
                                    {
                                        RootInfo parent = plantComponents.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponents[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;
                                        if (parent != null)
                                        {
                                            sgc.BeginFigure(points[points.Length - 2], false, false);
                                            sgc.LineTo(points[points.Length - 1], true, true);
                                        }
                                    }
                                    else
                                    {
                                        sgc.BeginFigure(points[points.Length - 2], false, false);
                                        sgc.LineTo(points[points.Length - 1], true, true);
                                    }
                                }
                                else
                                {
                                    RootInfo parent = plantComponents.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponents[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;

                                    if (parent != null)
                                    {
                                        Point start = parent.Spline.GetPoint(kvp.Value.StartReference);
                                        sgc.BeginFigure(new Point(start.X, start.Y), false, false);
                                        sgc.LineTo(new Point(points[0].X, points[0].Y), true, true);
                                    }
                                    else
                                    {
                                        sgc.BeginFigure(new Point(points[0].X, points[0].Y), false, false);
                                    }

                                    for (int i = 1; i < points.Length; i++)
                                    {
                                        sgc.LineTo(new Point(points[i].X, points[i].Y), true, true);
                                    }
                                }
                            }
                        }

                    }

                    if (geometry.CanFreeze)
                    {
                        geometry.Freeze();
                    }

                    drawingContext.DrawGeometry(p.Brush, p, geometry);

                    drawingContext.Close();

                    rtb.Render(drawingVisual);

                    if (rtb.CanFreeze)
                    {
                        rtb.Freeze();
                    }

                    // Save original file
                    BitmapEncoder Encoder;
                    if (outputOriginalImages)
                    {
                        Encoder = new PngBitmapEncoder();

                        using (FileStream FS = File.Open(imagePath + "\\" + count + ".png", FileMode.OpenOrCreate))
                        {
                            Encoder.Frames.Add(BitmapFrame.Create(useCrop ? new CroppedBitmap(currentImage, cropRectangle.Value) : currentImage));
                            Encoder.Save(FS);
                            FS.Close();
                        }
                    }

                    // Create mask from RTB
                    int renderStride = rtb.PixelWidth * 4;
                    byte[] renderArr = new byte[rtb.PixelWidth * rtb.PixelHeight * 4];
                    rtb.CopyPixels(renderArr, rtb.PixelWidth * 4, 0);

                    WriteableBitmap mask = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight, 96.0, 96.0, PixelFormats.Gray8, null);
                    mask.Lock();
                    byte* ptr = (byte*)mask.BackBuffer.ToPointer();
                    int stride = mask.BackBufferStride;

                    for (int x = 0; x < mask.PixelWidth; x++)
                    {
                        for (int y = 0; y < mask.PixelHeight; y++)
                        {
                            // Obtain red pixel as representative of gray value
                            byte current = renderArr[y * renderStride + (x * 4) + 1];
                            if (current > 127)
                            {
                                if (conflictOnly)
                                {
                                    int index = (y / conflictScale) * conflictWidth + (x / conflictScale);
                                    if (rootCount[index] > 1)
                                    {
                                        *(ptr + y * stride + x) = 255;
                                    }
                                }
                                else
                                {
                                    *(ptr + y * stride + x) = 255;
                                }
                            }
                        }
                    }

                    mask.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    mask.Unlock();

                    BitmapSource outputMask = mask;
                    Encoder = new PngBitmapEncoder();
                    using (FileStream FS = File.Open(segPath + "\\" + count.ToString("D4") + ".png", FileMode.OpenOrCreate))
                    {
                        Encoder.Frames.Add(BitmapFrame.Create(useCrop ? new CroppedBitmap(outputMask, cropRectangle.Value) : outputMask));
                        Encoder.Save(FS);
                        FS.Close();
                    }

                    count++;
                    runningTotal++;

                    if (runningTotal > 20)
                    {
                        runningTotal -= 20;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
        }
    }
}
