using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using RootNav.Data;

namespace RootNav.Viewer
{
    /// <summary>
    /// Interaction logic for RootNavViewerWindow.xaml
    /// </summary>
    public partial class RootNavViewerWindow : Window
    {
        private void extractImagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string[] supportedDatasets = new string[] { "RSDH00", "RSDH01", "RSDH02", "RSDH03", "RSDH04", "RSDH06", "RSDH07", "RSDH08", "RSDH09", "RSDH10" };
            //string[] supportedDatasets = new string[] { "RSDH06" };

            List<string> tags = rootReader.FilterTags(supportedDatasets, true);

          
            Random rand = new Random();

            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = "G:\\Training Sets\\root-systems"
            };
            var result = fbd.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string path = fbd.SelectedPath;
            string positivePath = path + "\\positive-42";
            string negativePath = path + "\\negative-42";

            //if (!Directory.Exists(positivePath)) Directory.CreateDirectory(positivePath);
            // if (!Directory.Exists(negativePath)) Directory.CreateDirectory(negativePath);

            // Directory selected, begin extracting images

            StreamReader strmrdr = new StreamReader(path + "\\mapped.txt");

            Dictionary<string, string> maps = new Dictionary<string, string>();
            while (!strmrdr.EndOfStream)
            {
                string line = strmrdr.ReadLine();
                string[] splits = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (splits[1] != "#N/A")
                {
                    string s = Path.GetFileName(splits[0]).Replace("JPG", "jpg");
                    maps.Add(splits[1].Trim(new char[] { '"' }), s);
                }
            }

            strmrdr.Close();

            const int DIM = 42;
            const int HALF = DIM / 2;

            Dictionary<int, int> labels = new Dictionary<int, int>();

            int positiveIndex = 0;
            int negativeIndex = 0;
            int runningTotal = 0;

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

                int sampleCount = 0;
                List<Point> ends = new List<Point>();
                foreach (PlantInfo pl in scene.Plants)
                {
                    /***************************************
                    ROOT TIPS
                    */
                    foreach (RootInfo ro in pl)
                    {

                        if (ro.Spline != null && ro.Spline.Length > HALF)
                        {
                            ends.Add(ro.Spline.End);
                            /*
                            int x = (int)(ro.Spline.End.X + 0.5);
                            int y = (int)(ro.Spline.End.Y + 0.5);

                            Int32Rect bounds = new Int32Rect(x - HALF, y - HALF, DIM, DIM);

                            if (bounds.X < 0) bounds.X = 0;
                            if (bounds.Y < 0) bounds.Y = 0;
                            if (bounds.X + bounds.Width >= currentImage.PixelWidth) bounds.X = currentImage.PixelWidth - bounds.Width - 1;
                            if (bounds.Y + bounds.Height >= currentImage.PixelHeight) bounds.Y = currentImage.PixelHeight - bounds.Height - 1;

                            CroppedBitmap cb = new CroppedBitmap(currentImage, bounds);

                            BitmapEncoder Encoder = new PngBitmapEncoder();

                            using (FileStream FS = File.Open(positivePath + "\\" + positiveIndex + ".png", FileMode.OpenOrCreate))
                            {
                                Encoder.Frames.Add(BitmapFrame.Create(cb));
                                Encoder.Save(FS);
                                FS.Close();
                            }

                            positiveIndex++;
                            sampleCount++;
                            */
                        }
                    }
                }

                ImageEncoder.SaveImage(path + "\\" + maps[tag].Replace("jpg", "png"), currentImage as WriteableBitmap, ImageEncoder.EncodingType.PNG);

                StreamWriter strm = new StreamWriter(path + "\\" + maps[tag].Replace("jpg", "txt"));
                foreach (Point p in ends)
                {
                    strm.WriteLine(p.ToString());
                }
                strm.Close();

                int rootNegativeSampleCount = (sampleCount * 2) / 2;
                int randomNegativeSampleCount = (sampleCount * 2) - rootNegativeSampleCount;


                /***************************************
                ROOT NEGATIVE SAMPLES
                */
                /*
                List<RootInfo> roots = new List<RootInfo>();
                List<double> cumulativeLength = new List<double>();

                // Prepare roots
                foreach (PlantInfo pl in scene.Plants)
                {
                    foreach (RootInfo ro in pl.Roots)
                    {
                        //if (ro.Spline.Length > 10)
                        //{
                            roots.Add(ro);
                            if (cumulativeLength.Count == 0)
                                cumulativeLength.Add(ro.Spline.Length);
                            else
                                cumulativeLength.Add(ro.Spline.Length + cumulativeLength[cumulativeLength.Count - 1]);
                        //}
                    }
                }

                for (int i = 0; i < rootNegativeSampleCount; i++)
                {
                
                    // Select root at random
                    double position = rand.NextDouble() * cumulativeLength[cumulativeLength.Count - 1];

                    int rootIndex = 0;
                    for (int p = 0; p < cumulativeLength.Count; p++)
                    {
                        if (position <= cumulativeLength[p])
                        {
                            rootIndex = p;
                            break;
                        }
                    }

                    RootInfo selectedRoot = roots[rootIndex];
                    double selectedPosition = selectedRoot.Spline.Length < DIM ? 0 : rand.NextDouble() * (selectedRoot.Spline.Length - DIM);
                    Point pos = selectedRoot.Spline.GetPoint(selectedRoot.Spline.GetPositionReference(selectedPosition));

                    int x = (int)(pos.X + 0.5);
                    int y = (int)(pos.Y + 0.5);

                    Int32Rect bounds = new Int32Rect(x - HALF, y - HALF, DIM, DIM);

                    if (bounds.X < 0) bounds.X = 0;
                    if (bounds.Y < 0) bounds.Y = 0;
                    if (bounds.X + bounds.Width >= currentImage.PixelWidth) bounds.X = currentImage.PixelWidth - bounds.Width - 1;
                    if (bounds.Y + bounds.Height >= currentImage.PixelHeight) bounds.Y = currentImage.PixelHeight - bounds.Height - 1;

                    CroppedBitmap cb = new CroppedBitmap(currentImage, bounds);

                    BitmapEncoder Encoder = new PngBitmapEncoder();

                    using (FileStream FS = File.Open(negativePath + "\\" + negativeIndex + ".png", FileMode.OpenOrCreate))
                    {
                        Encoder.Frames.Add(BitmapFrame.Create(cb));
                        Encoder.Save(FS);
                        FS.Close();
                    }

                    negativeIndex++;
                }
                */
                /*
                for (int i = 0; i < randomNegativeSampleCount; i++)
                {
                    /***************************************
                    RANDOM NEGATIVE SAMPLES
                    */
                    /*
                    int x, y;

                    bool validPosition;

                    do
                    {
                        validPosition = true;
                        x = rand.Next(0, currentImage.PixelWidth - DIM - 1);
                        y = rand.Next(0, currentImage.PixelWidth - DIM - 1);

                        foreach (PlantInfo pl in scene.Plants)
                        {
                            if (!validPosition) break;

                            foreach (RootInfo ro in pl)
                            {
                                if (ro.Spline.Length < DIM) continue;

                                int rX = (int)ro.Spline.End.X;
                                int rY = (int)ro.Spline.End.Y;

                                if (x >= rX && x <= rX + DIM && y >= rY && y <= rY + DIM)
                                {
                                    validPosition = false;
                                    break;
                                }
                            }
                        }
                    } while (!validPosition);

                    Int32Rect bounds = new Int32Rect(x - HALF, y - HALF, DIM, DIM);

                    if (bounds.X < 0) bounds.X = 0;
                    if (bounds.Y < 0) bounds.Y = 0;
                    if (bounds.X + bounds.Width >= currentImage.PixelWidth) bounds.X = currentImage.PixelWidth - bounds.Width - 1;
                    if (bounds.Y + bounds.Height >= currentImage.PixelHeight) bounds.Y = currentImage.PixelHeight - bounds.Height - 1;

                    CroppedBitmap cb = new CroppedBitmap(currentImage, bounds);

                    BitmapEncoder Encoder = new PngBitmapEncoder();

                    using (FileStream FS = File.Open(negativePath + "\\" + negativeIndex + ".png", FileMode.OpenOrCreate))
                    {
                        Encoder.Frames.Add(BitmapFrame.Create(cb));
                        Encoder.Save(FS);
                        FS.Close();
                    }

                    negativeIndex++;
                }
                */
                runningTotal += sampleCount * 2;

                if (runningTotal > 400)
                {
                    runningTotal -= 400;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }
    }
}
