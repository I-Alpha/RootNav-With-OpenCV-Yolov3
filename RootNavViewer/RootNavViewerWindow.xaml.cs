using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Data;

using RootNav.Measurement;
using RootNav.Data;
using RootNav.Data.IO;
using RootNav.Data.IO.Databases;
using RootNav.Data.IO.RSML;

namespace RootNav.Viewer
{
    
    /// <summary>
    /// Interaction logic for RootNavViewerWindow.xaml
    /// </summary>
    public partial class RootNavViewerWindow : Window
    {
        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register("Connected", typeof(bool), typeof(RootNavViewerWindow), new PropertyMetadata(false));

        public bool Connected
        {
            get
            {
                return (bool)GetValue(ConnectedProperty);
            }
            set
            {
                SetValue(ConnectedProperty, value);
            }
        }

        public static readonly DependencyProperty ImageProperty =
          DependencyProperty.Register("Image", typeof(ImageSource), typeof(RootNavViewerWindow), new PropertyMetadata(null));

        public ImageSource Image
        {
            get
            {
                return (ImageSource)GetValue(ImageProperty);
            }
            set
            {
                SetValue(ImageProperty, value);
            }
        }

        List<MeasurementHandler> handlers;

        private List<string> plantTags = null;
        private List<string> unfilteredPlantTags = null;

        public List<string> PlantTags
        {
            get { return plantTags; }
            set { plantTags = value; }
        }

        private int currentPlantIndex = 0;
        private SceneMetadata currentMetadata = new SceneMetadata();
        private SceneInfo currentScene = new SceneInfo();
        ConnectionParams connectionInfo = null;

        private AnnotationAdorner annotationOverlay = null;

        public RootNavViewerWindow()
        {
            InitializeComponent();

            AdornerLayer AL = AdornerLayer.GetAdornerLayer(this.mainImage);
            this.annotationOverlay = new AnnotationAdorner(this.mainImage, null);
            AL.Add(this.annotationOverlay);

            if (System.Reflection.Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture.ToString().Contains("64"))
            {
                this.Title += " (64-bit)";
            }
            
            connectionInfo = ConnectionParams.FromEncryptedStorage();
            if (connectionInfo == null)
            {
                DataSourceWindow dsw = new DataSourceWindow();
                bool? result = dsw.ShowDialog();
                if ((bool)result)
                {
                    connectionInfo = dsw.ConnectionInfo;
                }
            }

            try
            {
                this.handlers = PluginLoader.GetPlugins<MeasurementHandler>(Directory.GetCurrentDirectory());

                foreach (MeasurementHandler handler in this.handlers)
                {
                    if (handler.Measures == MeasurementType.Plant)
                    {
                        this.plantListBox.Items.Add(handler);
                    }
                    else
                    {
                        this.rootListBox.Items.Add(handler);
                    }
                }

                if (connectionInfo != null)
                {
                    InitialiseConnection();   
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception caught:" + e.Message);
            }
        }

        void InitialiseConnection()
        {
            currentPlantIndex = 0;
            currentScene = new SceneInfo() { Plants = new List<PlantInfo>() };
            this.Image = null;

            // Background worker to use database connection
            BackgroundWorker bw = new BackgroundWorker() { WorkerReportsProgress = false };
            bw.DoWork += ConnectWorkerDoWork;
            bw.RunWorkerCompleted += ConnectWorkerCompleted;
            bw.RunWorkerAsync();
        }

        void ConnectWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                object[] results = (object[])e.Result;
                this.plantTags = (List<string>)results[0];

                this.currentMetadata = (SceneMetadata)results[1];
                this.currentScene = (SceneInfo)results[2];
                byte[] retrievedImage = (byte[])results[3];

                UpdateScene(true, this.currentMetadata, this.currentScene, retrievedImage);
                UpdateRootTags(true, "database");

                this.statusText.Text = "Status: Connected to database";
                this.Connected = true;
            }
            else
            {
                this.plantTags = new List<String>();
                this.currentScene = new SceneInfo() { Plants = new List<PlantInfo>() };
                this.currentMetadata = new SceneMetadata();
                this.Image = null;
                this.annotationOverlay.Scene = null;
                this.rootCountTextBlock.Text = "";

                this.UpdateScene(false, this.currentMetadata, this.currentScene, null);
                UpdateRootTags(true, "database");
                
                this.statusText.Text = "Status: Not connected";
                this.Connected = false;
            }
        }

        public void UpdateRootTags(bool updateCount, string source)
        {
            if (updateCount)
                this.rootCountTextBlock.Text = string.Format("{0:n0} plants found in {1}", this.plantTags.Count, source);

            if (this.currentScene == null || this.currentMetadata == null || this.currentScene.Plants == null || this.currentScene.Plants.Count == 0)
            {
                this.rootImageTagTextBlock.Text = "";
                this.RootNumberLabel.Content = "";
            }
            else
            {
                this.traitsTagTextblock.Text = this.currentMetadata.Key;

                if (this.currentMetadata.Image != null && this.currentMetadata.Image.Captured.HasValue)
                {
                    this.traitsDateTextbox.Text = this.currentMetadata.Image.Captured.Value.ToString("d");
                }
                else
                {
                    this.traitsDateTextbox.Text = "Unspecified";
                }


                this.traitsPlantCountTextbox.Text = this.currentScene.Plants.Count.ToString();

                this.rootImageTagTextBlock.Text = string.Format("Current Root: {0}", this.currentMetadata.Key);
                this.RootNumberLabel.Content = string.Format("{0}/{1}", currentPlantIndex + 1, plantTags.Count);
            }
        }

        void ConnectWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            if (connectionInfo.Source == ConnectionSource.MySQLDatabase)
            {
                rootReader = new MySQLRootReader(connectionInfo);
                rootReader.Initialise();
            }
            else if (connectionInfo.Source == ConnectionSource.RSMLDirectory)
            {
                rootReader = new RSMLRootReader(connectionInfo);
                rootReader.Initialise();
            }
         
            List<string> tags = null;
            SceneInfo currentScene = null;
            SceneMetadata currentMetadata = null;

            if (rootReader.Connected)
            {
                tags = rootReader.ReadAllTags();

                if (tags.Count > 0)
                {
                    var tuple = rootReader.Read(tags[currentPlantIndex], true);
                    currentMetadata = tuple.Item1;
                    currentScene = tuple.Item2;
                    e.Result = new object[] { tags, tuple.Item1, tuple.Item2, tuple.Item3  };
                }
            }
        }

        private IRootReader rootReader;

        private ImageSource CreatePlantImage(List<PlantInfo> plants, out Point sourcePosition, Brush foreground, Brush background, BitmapSource backgroundImage = null)
        {
            bool UseBackgroundDimensions = backgroundImage != null;

            sourcePosition = default(Point);

            Pen p = new Pen(foreground, 6.0);
            p.StartLineCap = PenLineCap.Round;
            p.EndLineCap = PenLineCap.Round;

            Dictionary<String, RootInfo> plantComponents = new Dictionary<string, RootInfo>();

            foreach (PlantInfo plant in plants)
            {
                foreach (RootInfo root in plant)
                {
                    // Start of first primary is the plant source
                    if (root.RelativeID == "1.1")
                    {
                        sourcePosition = new Point(root.Spline.Start.X, root.Spline.Start.Y);
                    }
                    plantComponents.Add(root.RelativeID, root);
                }
            }

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
            }


            int width, height;

            if (UseBackgroundDimensions)
            {
                width = backgroundImage.PixelWidth;
                height = backgroundImage.PixelHeight;
            }
            else
            {
                width = (int)right - (int)left + 8;
                height = (int)bottom - (int)top + 8;
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            
            if (background != null)
                drawingContext.DrawRectangle(background, null, new Rect(0, 0, width, height));

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

                        RootInfo parent = plantComponents.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponents[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;

                        if (parent != null)
                        {
                            Point start = parent.Spline.GetPoint(kvp.Value.StartReference);
                            if (UseBackgroundDimensions)
                            {
                                sgc.BeginFigure(new Point(start.X, start.Y), false, false);
                                sgc.LineTo(new Point(points[0].X, points[0].Y), true, true);
                            }
                            else
                            {
                                sgc.BeginFigure(new Point(start.X - left + 4, start.Y - top + 4), false, false);
                                sgc.LineTo(new Point(points[0].X - left + 4, points[0].Y - top + 4), true, true);
                            }
                        }
                        else
                        {
                            if (UseBackgroundDimensions)
                                sgc.BeginFigure(new Point(points[0].X, points[0].Y), false, false);
                            else
                                sgc.BeginFigure(new Point(points[0].X - left + 4, points[0].Y - top + 4), false, false);
                        }

                        for (int i = 1; i < points.Length; i++)
                        {
                            if (UseBackgroundDimensions)
                                sgc.LineTo(new Point(points[i].X, points[i].Y), true, true);
                            else
                                sgc.LineTo(new Point(points[i].X - left + 4, points[i].Y - top + 4), true, true);
                        }
                    }
                }
            }

            if (geometry.CanFreeze)
            {
                geometry.Freeze();
            }

            drawingContext.DrawGeometry(null, p, geometry);

            if (UseBackgroundDimensions)
            {
                sourcePosition.X = sourcePosition.X;
                sourcePosition.Y = sourcePosition.Y;
            }
            else
            {
                sourcePosition.X = sourcePosition.X - left + 4;
                sourcePosition.Y = sourcePosition.Y - top + 4;
            }

            drawingContext.Close();

            if (UseBackgroundDimensions)
            {
                this.annotationOverlay.Left = 0;
                this.annotationOverlay.Top = 0;
            }
            else
            {
                this.annotationOverlay.Left = -left + 4;
                this.annotationOverlay.Top = -top + 4;
            }

            rtb.Render(drawingVisual);

            if (rtb.CanFreeze)
            {
                rtb.Freeze();
            }

            return rtb;
        }

        private List<ImageSource> CreateSeparatePlantImages(List<PlantInfo> plants, out List<Point> sourcePositions)
        {
            sourcePositions = new List<Point>();
            var outputImages = new List<ImageSource>();

            Pen p = new Pen(Brushes.Black, 6.0);
            p.StartLineCap = PenLineCap.Round;
            p.EndLineCap = PenLineCap.Round;

            List<Dictionary<String,RootInfo>> plantComponents = new List<Dictionary<string,RootInfo>>();

            int index = 0;
            foreach (PlantInfo plant in plants)
            {
                plantComponents.Insert(index, new Dictionary<string, RootInfo>());
                foreach (RootInfo root in plant)
                {
                    // Start of first primary is the plant source
                    if (root.RelativeID == plant.RelativeID + ".1")
                    {
                        sourcePositions.Add(new Point(root.Spline.Start.X, root.Spline.Start.Y));
                    }
                    plantComponents[index].Add(root.RelativeID, root);
                }
                index++;
            }

            // For each plant, create a new image
            for (int i = 0; i < plantComponents.Count; i++)
            {
                var plantComponentDictionary = plantComponents[i];

                // Find global plant bounding box
                double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;

                foreach (var components in plantComponentDictionary.Values)
                {
                    SampledSpline spline = components.Spline as SampledSpline;
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
                }

                int width = (int)right - (int)left + 8;
                int height = (int)bottom - (int)top + 8;

                RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();

                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                // Draw splines
                StreamGeometry geometry = new StreamGeometry();

                using (StreamGeometryContext sgc = geometry.Open())
                {
                    foreach (var kvp in plantComponentDictionary)
                    {
                        SampledSpline spline = kvp.Value.Spline as SampledSpline;
                        if (spline != null)
                        {
                            Point[] points = spline.SampledPoints;

                            // Optional line from start position on parent
                            RootInfo parent = plantComponentDictionary.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponentDictionary[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;

                            if (parent != null)
                            {
                                Point start = parent.Spline.GetPoint(kvp.Value.StartReference);
                                sgc.BeginFigure(new Point(start.X - left + 4, start.Y - top + 4), false, false);
                                sgc.LineTo(new Point(points[0].X - left + 4, points[0].Y - top + 4), true, true);
                            }
                            else
                            {
                                sgc.BeginFigure(new Point(points[0].X - left + 4, points[0].Y - top + 4), false, false);
                            }

                            for (int i2 = 1; i2 < points.Length; i2++)
                            {
                                sgc.LineTo(new Point(points[i2].X - left + 4, points[i2].Y - top + 4), true, true);
                            }
                        }
                    }
                }

                if (geometry.CanFreeze)
                {
                    geometry.Freeze();
                }

                drawingContext.DrawGeometry(null, p, geometry);

                sourcePositions[i] = new Point(sourcePositions[i].X - left + 4, sourcePositions[i].Y - top + 4);

                drawingContext.Close();

                this.annotationOverlay.Left = -left + 4;
                this.annotationOverlay.Top = -top + 4;

                rtb.Render(drawingVisual);

                if (rtb.CanFreeze)
                {
                    rtb.Freeze();
                }

                outputImages.Add(rtb);
            }

            return outputImages;
        }
        
        private void UpdateScene(bool enableUI, SceneMetadata metadata, SceneInfo scene, byte[] retrievedImage)
        {
            if (!enableUI)
            {
                this.Image = null;

                this.rootInformationPanel.IsEnabled = false;
                this.traitsPanel.IsEnabled = false;
                return;
            }
            else
            {
                this.currentMetadata = metadata;
                this.currentScene = scene;

                // Is there a supplied background image?
                BitmapSource rImage = null;
                if (retrievedImage != null)
                {
                    using (MemoryStream ms = new MemoryStream(retrievedImage))
                    {
                        JpegBitmapDecoder decoder = new JpegBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        // Due to what appears to be a bug in .NET, wrapping in a WBMP causes a cache refresh and fixes some image corruption
                        rImage = new WriteableBitmap(decoder.Frames[0]);
                    }
                }
               
                Point source;
                this.Image = CreatePlantImage(scene.Plants, out source, retrievedImage == null ? Brushes.Black : new SolidColorBrush(Color.FromArgb(225, 190,104,232)), retrievedImage == null ? Brushes.White : null, rImage);
                this.backgroundImage.Source = rImage;
                this.annotationOverlay.Scene = this.currentScene;
                this.rootInformationPanel.IsEnabled = true;
                this.traitsPanel.IsEnabled = true;
            }
        }

        private void LeftNavClick(object sender, RoutedEventArgs e)
        {
            if (this.plantTags.Count == 0)
            {
                return;
            }

            this.currentPlantIndex = Math.Max(currentPlantIndex - 1, 0);
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += PlantIndexChangedDoWork;
            bw.RunWorkerCompleted += PlantIndexChangedWorkerCompleted;
            bw.RunWorkerAsync();

            
        }

        private void RightNavClick(object sender, RoutedEventArgs e)
        {
            if (this.plantTags.Count == 0)
            {
                return;
            }

            this.currentPlantIndex = Math.Min(currentPlantIndex + 1, this.plantTags.Count - 1);
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += PlantIndexChangedDoWork;
            bw.RunWorkerCompleted += PlantIndexChangedWorkerCompleted;
            bw.RunWorkerAsync();
        }

        void PlantIndexChangedDoWork(object sender, DoWorkEventArgs e)
        {
            String tag = plantTags[currentPlantIndex];

            if (plantTags.Count > 0)
            { 
                Tuple<SceneMetadata, SceneInfo, byte[]> result = rootReader.Read(tag, true);
                e.Result = new object[] { result.Item1, result.Item2, result.Item3 };
            }
        }

        void PlantIndexChangedWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                object[] results = (object[])e.Result;
                this.currentMetadata = (SceneMetadata)results[0];
                this.currentScene = (SceneInfo)results[1];
                byte[] retrievedImage = (byte[])results[2];

                UpdateScene(true, currentMetadata, currentScene, retrievedImage);
                UpdateRootTags(false, "");
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // Backup unfiltered tags if required
            if (unfilteredPlantTags == null)
            {
                unfilteredPlantTags = plantTags;
            }

            BackgroundWorker bw = new BackgroundWorker() { WorkerReportsProgress = false };
            bw.DoWork += SearchWorkerDoWork;
            bw.RunWorkerCompleted += SearchWorkerCompleted;

            String[] searchTerms = this.SearchBox.Text.Split(' ');
            List<object> parameterList = new List<object>();

            parameterList.Add(searchTerms);
            parameterList.Add((bool)this.AnyRadioButton.IsChecked);

            DateTime? dt = null;
            DateTime parsedDateTime;
            if (DateTime.TryParse(this.DateBox.Text, out parsedDateTime))
            {
                dt = parsedDateTime;
            }

            parameterList.Add(dt);

            bw.RunWorkerAsync(parameterList);
        }

        private void DuplicateTagRemoval(List<PlantInfo> plants)
        {
            HashSet<DateTime> stamps = new HashSet<DateTime>();

            foreach (PlantInfo plant in plants)
            {
                stamps.Add(plant.Stamp);
            }

            if (stamps.Count > 1)
            {
                DateTime minimum = stamps.Min();

                for (int i = plants.Count - 1; i >= 0; i--)
                {
                    if (plants[i].Stamp != minimum)
                    {
                        plants.RemoveAt(i);
                    }
                }

            }
        }

        void SearchWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            object[] results = (object[])e.Result;

            // Set plant tags to be new tags, or failing that, an empty list
            plantTags = results[0] as List<string> != null ? results[0] as List<string> : new List<string>();

            if (this.plantTags.Count > 0)
            {
                currentPlantIndex = 0;
                Tuple<SceneMetadata,SceneInfo, byte[]> result = rootReader.Read(this.plantTags[currentPlantIndex], true);
                UpdateScene(true, result.Item1, result.Item2, result.Item3);
                UpdateRootTags(true, "search");
            }
            else
            {
                UpdateScene(false, null, null, null);
                UpdateRootTags(true, "search");
            }

            this.ClearBorder.Visibility = System.Windows.Visibility.Visible;
        }

        void SearchWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            List<object> args = e.Argument as List<object>;

            if (args == null)
            {
                e.Result = null;
                return;
            }

            List<string> tags = rootReader.FilterTags((String[])args[0], (bool)args[1], (DateTime?)args[2]);

            e.Result = new object[] { tags };
        }

        private void ClearBorder_Click(object sender, RoutedEventArgs e)
        {
            this.currentScene = null;
            this.currentMetadata = null;

            this.plantTags = unfilteredPlantTags;

            if (this.plantTags.Count > 0)
            {
                currentPlantIndex = 0;
                Tuple<SceneMetadata, SceneInfo, byte[]> result = rootReader.Read(this.plantTags[currentPlantIndex],true);
                this.currentMetadata = result.Item1;
                this.currentScene = result.Item2;
                UpdateScene(true, this.currentMetadata, this.currentScene, result.Item3);
                UpdateRootTags(true, "database");
            }
            else
            {
                UpdateRootTags(false, "");
            }

            this.ClearBorder.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Measure_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog();

            // Create null culture info for safe string conversions.
            System.Globalization.CultureInfo nullInfo = new System.Globalization.CultureInfo("");

            // Separate the list of handlers into those that output single values and 2D data
            List<MeasurementHandler> singleHandlers = new List<MeasurementHandler>();
            List<MeasurementHandler> matrixHandlers = new List<MeasurementHandler>();

            foreach (MeasurementHandler handler in this.plantListBox.SelectedItems)
            {
                if (handler.ReturnsSingleItem)
                {
                    singleHandlers.Add(handler);
                }
                else
                {
                    matrixHandlers.Add(handler);
                }
            }

            foreach (MeasurementHandler handler in this.rootListBox.SelectedItems)
            {
                if (handler.ReturnsSingleItem)
                {
                    singleHandlers.Add(handler);
                }
                else
                {
                    matrixHandlers.Add(handler);
                }
            }

            // Single handlers
            if (singleHandlers.Count > 0)
            {
                IDataOutputHandler singleOutputPlants = null;
                IDataOutputHandler singleOutputRoots = null;

                bool handlesPlants = false, handlesRoots = false;

                foreach (MeasurementHandler handler in singleHandlers)
                {
                    handlesPlants = handlesPlants || handler.Measures == MeasurementType.Plant;
                    handlesRoots = handlesRoots || handler.Measures == MeasurementType.Root;
                }

                if ((bool)this.TabularCheckbox.IsChecked)
                {
                    if (handlesPlants)
                    {
                        singleOutputPlants = new MeasurementWindow() { Title = "Plant Measurements" };
                    }

                    if (handlesRoots)
                    {
                        singleOutputRoots = new MeasurementWindow() { Title = "Root Measurements" };
                    }
                }
                else
                {
                    if (handlesPlants)
                    {
                        singleOutputPlants = new CSVMeasurementWriter();
                    }

                    if (handlesRoots)
                    {
                        singleOutputRoots = new CSVMeasurementWriter();
                    }
                }

                // Measure each plant, and if necessary each root
                foreach (String tag in this.plantTags)
                {
                    Tuple<SceneMetadata, SceneInfo, byte[]> result = rootReader.Read(tag, false);
                    SceneMetadata metadata = result.Item1;
                    SceneInfo scene = result.Item2;
                    List<PlantInfo> currentPlants = scene.Plants;
                    int plantID = 1;

                    foreach (PlantInfo currentPlant in currentPlants)
                    {
                        if (handlesPlants)
                        {
                            Dictionary<String, String> measurements = new Dictionary<string, string>();

                            // First column, plant IDs.
                            if (currentPlants.Count > 1)
                            {
                                measurements.Add("Tag", "\"" + (currentPlant.Tag != null ? currentPlant.Tag : metadata.Key) + ":" + plantID.ToString() + "\"");
                                plantID++;
                            }
                            else
                            {
                                measurements.Add("Tag", "\"" + (currentPlant.Tag != null ? currentPlant.Tag : metadata.Key) + "\"");
                            }

                            foreach (MeasurementHandler handler in singleHandlers)
                            {
                                try
                                {
                                    if (handler.Measures == MeasurementType.Plant)
                                    {
                                        object handlerResult =  handler.MeasurePlant(currentPlant);
                                        if (handlerResult is string)
                                            measurements.Add(handler.Name, handlerResult as string);
                                        else
                                        {
                                            string s = string.Format(nullInfo, "{0}", handlerResult);
                                            measurements.Add(handler.Name, s);
                                        }
                                    }
                                }
                                catch
                                {
                                    measurements.Add(handler.Name, "Error: Could not measure");
                                }
                            }

                            if ((bool)completedArchitecturesCheckbox.IsChecked)
                            {
                                measurements.Add("Complete", currentPlant.Complete ? "Yes" : "No");
                            }

                            singleOutputPlants.Add(measurements);
                        }

                        if (handlesRoots)
                        {
                            foreach (RootInfo root in currentPlant)
                            {
                                Dictionary<String, String> measurements = new Dictionary<string, string>();

                                // First column, plant ID + relativeID.
                                measurements.Add("Tag", "\"" + (currentPlant.Tag != null ? currentPlant.Tag : metadata.Key) + ":" + root.RelativeID + "\"");

                                foreach (MeasurementHandler handler in singleHandlers)
                                {
                                    if (handler.Measures == MeasurementType.Root)
                                    {
                                        try
                                        {
                                            object handlerResult = handler.MeasureRoot(root, currentPlant.GetParent(root));
                                            if (handlerResult is string)
                                                measurements.Add(handler.Name, handlerResult as string);
                                            else
                                            {
                                                string s = string.Format(nullInfo, "{0}", handlerResult);
                                                measurements.Add(handler.Name, s);
                                            }
                                        }
                                        catch
                                        {
                                            measurements.Add(handler.Name, "Error: Could not measure");
                                        }

                                    }
                                }

                                if ((bool)completedArchitecturesCheckbox.IsChecked)
                                {
                                    measurements.Add("Complete", currentPlant.Complete ? "Yes" : "No");
                                }

                                singleOutputRoots.Add(measurements);
                            }
                        }


                    }
                }

                if ((bool)this.TabularCheckbox.IsChecked)
                {
                    if (handlesPlants)
                    {
                        MeasurementWindow plantWindow = singleOutputPlants as MeasurementWindow;
                        if (plantWindow != null)
                        {
                            plantWindow.Show();
                        }
                    }
                    if (handlesRoots)
                    {
                        MeasurementWindow rootWindow = singleOutputRoots as MeasurementWindow;
                        rootWindow.Show();
                    }
                }
                else
                {
                    CSVMeasurementWriter plantWriter = singleOutputPlants as CSVMeasurementWriter;
                    CSVMeasurementWriter rootWriter = singleOutputRoots as CSVMeasurementWriter;

                    System.Windows.Forms.DialogResult result = d.ShowDialog();


                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        String folder = d.SelectedPath;

                        if (plantWriter != null)
                        {
                            plantWriter.Write(folder + "\\Plant measurements.csv");
                        }

                        if (rootWriter != null)
                        {
                            rootWriter.Write(folder + "\\Root measurements.csv");
                        }
                    }
                }
            }

            if (matrixHandlers.Count > 0)
            {

                // Unlike single output, each 2D handler will output to a separate table.
                foreach (MeasurementHandler handler in matrixHandlers)
                {
                    List<List<object>> measurements = new List<List<object>>();

                    if (handler.Measures == MeasurementType.Plant)
                    {
                        foreach (String tag in this.plantTags)
                        {
                            Tuple<SceneMetadata, SceneInfo, byte[]> result = rootReader.Read(tag, false);
                            SceneMetadata metadata = result.Item1;
                            SceneInfo scene = result.Item2;
                            List<PlantInfo> currentPlants = scene.Plants;
                            int plantID = 1;

                            foreach (PlantInfo plant in currentPlants)
                            {
                                if (measurements.Count == 0)
                                {
                                    // For the first plant, begin measurement lists
                                    List<List<object>> currentData = (List<List<object>>)handler.MeasurePlant(plant);

                                    // Independent axis
                                    measurements.Add(currentData[0]);

                                    // Rename dependent axis depending on tag
                                    currentData[1][0] = (plant.Tag != null ? plant.Tag : metadata.Key) + ":" + plantID.ToString();
                                    measurements.Add(currentData[1]);

                                    // Rename Measurement column to plant tag
                                    plantID++;
                                }
                                else
                                {
                                    // For subsequent plants, add only data, not independent axis
                                    List<List<object>> currentData = (List<List<object>>)handler.MeasurePlant(plant);
                                    
                                    currentData[1][0] = plant.Tag + ":" + plantID.ToString();
                                    plantID++;

                                    // Merge tables
                                    measurements.Add(currentData[1]);
                                }
                            }
                        }

                    }
                    else if (handler.Measures == MeasurementType.Root)
                    {
                        foreach (String tag in this.plantTags)
                        {
                            Tuple<SceneMetadata, SceneInfo, byte[]> result = rootReader.Read(tag, false);
                            SceneMetadata metadata = result.Item1;
                            SceneInfo scene = result.Item2;
                            List<PlantInfo> currentPlants = scene.Plants;

                            foreach (PlantInfo plant in currentPlants)
                            {
                                foreach (RootInfo root in plant)
                                {
                                    if (measurements.Count == 0)
                                    {

                                        // For the first plant, begin measurement lists
                                        List<List<object>> currentData = (List<List<object>>)handler.MeasureRoot(root);

                                        // Independent axis
                                        measurements.Add(currentData[0]);

                                        // Rename dependent axis depending on tag
                                        currentData[1][0] = (plant.Tag != null ? plant.Tag : metadata.Key) + ":" + root.RelativeID;
                                        measurements.Add(currentData[1]);
                                    }
                                    else
                                    {
                                        // For subsequent plants, add only data, not independent axis
                                        List<List<object>> currentData = (List<List<object>>)handler.MeasureRoot(root);

                                        currentData[1][0] = (plant.Tag != null ? plant.Tag : metadata.Key) + ":" + root.RelativeID;
                                        measurements.Add(currentData[1]);
                                    }
                                }
                            }
                        }

                    }

                    // Current output format (List of lists of objects) is inefficient. Reconstruct as arrays.

                    List<object[]> arrayStageOne = new List<object[]>();
                    int max = int.MinValue;
                    foreach (List<object> lst in measurements)
                    {
                            arrayStageOne.Add(lst.ToArray());
                            
                        if (lst.Count > max)
                        {
                            max = lst.Count;
                        }
                    }

                    object[][] outputArray = arrayStageOne.ToArray();

                    // Matrix data is output using this code, rather than the CSVMeasurementWriter
                    StringBuilder sb = new StringBuilder();
                  
                    for (int row = 0; row < max; row++)
                    {
                        List<string> rowValues = new List<string>();
                        for (int col = 0; col < outputArray.Length; col++)
                        {
                            if (outputArray[col].Length > row)
                            {
                                if (outputArray[col][row] is string)
                                {
                                    // Surround strings in quotes
                                    rowValues.Add("\"" + (outputArray[col][row] as string) + "\"");
                                }
                                else
                                {
                                    rowValues.Add(string.Format(nullInfo, "{0}", outputArray[col][row]));
                                }
                            }
                            else
                            {
                                rowValues.Add("");
                            }
                        }
                        sb.AppendLine(string.Join(",", rowValues));
                    }

                    if (d.SelectedPath == "")
                    {
                        System.Windows.Forms.DialogResult result = d.ShowDialog();

                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            File.WriteAllText(d.SelectedPath + "\\" + handler.Name + ".csv", sb.ToString());
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        File.WriteAllText(d.SelectedPath + "\\" + handler.Name + ".csv", sb.ToString());
                    }
                }
            }

        }

        System.Windows.Forms.SaveFileDialog svfd = null;
        System.Windows.Forms.FolderBrowserDialog fbrd = null;

        #region Export CSV / XML
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)this.ExportCSVCheckbox.IsChecked)
            {
                if (svfd == null)
                {
                    svfd = new System.Windows.Forms.SaveFileDialog()
                    {
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        Filter = "Text File (*.txt)|*.txt"
                    };
                }

                if (svfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Dictionary<String, List<Point>> splinePoints = new Dictionary<string, List<Point>>();
                    StreamWriter strm = new StreamWriter(svfd.FileName, false);

                    foreach (String tag in this.plantTags)
                    {
                        Tuple<SceneMetadata, SceneInfo, byte[]> result = rootReader.Read(tag, false);
                        SceneMetadata metadata = result.Item1;
                        SceneInfo scene = result.Item2;
                        List<PlantInfo> currentPlants = scene.Plants;

                        foreach (PlantInfo currentPlant in currentPlants)
                        {
                            foreach (RootInfo root in currentPlant)
                            {
                                if (root.Spline != null)
                                {
                                    StringBuilder currentLine = new StringBuilder();
                                    currentLine.AppendFormat("{0} {1}:", metadata.Key, root.RelativeID);

                                    for (int i = 0; i < root.Spline.SampledPoints.Length; i++)
                                    {
                                        if (i < root.Spline.SampledPoints.Length - 1)
                                        {
                                            currentLine.AppendFormat("{0},{1} ", Math.Round(root.Spline.SampledPoints[i].X, 2), Math.Round(root.Spline.SampledPoints[i].Y, 2));
                                        }
                                        else
                                        {
                                            currentLine.AppendFormat("{0},{1}", Math.Round(root.Spline.SampledPoints[i].X, 2), Math.Round(root.Spline.SampledPoints[i].Y, 2));
                                        }
                                    }

                                    strm.WriteLine(currentLine.ToString());
                                }
                            }
                        }
                    }

                    strm.Close();
                }
            }
            else if ((bool)this.ExportXMLCheckbox.IsChecked)
            {
                if (fbrd == null)
                {
                    fbrd = new System.Windows.Forms.FolderBrowserDialog()
                    {
                        RootFolder = Environment.SpecialFolder.Desktop
                    };
                }

                if (fbrd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    RSMLRootWriter writer = new RSMLRootWriter(new ConnectionParams() { Directory = fbrd.SelectedPath });
                    foreach (String tag in this.plantTags)
                    {
                        Tuple<SceneMetadata, SceneInfo, byte[]> result = rootReader.Read(tag, false);
                        SceneMetadata metadata = result.Item1;
                        SceneInfo scene = result.Item2;

                        writer.Write(metadata, scene);
                    }
                }
            }
        }
        #endregion

        #region Output image code
        unsafe private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            int option = CustomQueryBox.Show("Image Output", "Which type of image would you like to output?", "Combined Image", "Separate Images");

            System.Windows.Forms.FolderBrowserDialog d;
            String path;
            System.Windows.Forms.DialogResult result;

            switch (option)
            {
                case 1: // Combined image
                    d = new System.Windows.Forms.FolderBrowserDialog();
                    result = d.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        path = d.SelectedPath;
                    }
                    else
                    {
                        return;
                    }

                    int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
                    AccumulatorGrid grid = new AccumulatorGrid();

                    DateTime dt = DateTime.Now;

                    for (int i = 0; i < this.plantTags.Count; i++)
                    {
                        String tag = this.plantTags[i];

                        try
                        {
                            Tuple<SceneMetadata, SceneInfo, byte[]> result2 = rootReader.Read(tag, false);
                            SceneMetadata metadata = result2.Item1;
                            SceneInfo scene = result2.Item2;
                            List<PlantInfo> currentPlants = scene.Plants;

                            List<Point> sourcePositions;
                            // Create image and convert to writeablebitmap
                            List<ImageSource> imgs = CreateSeparatePlantImages(currentPlants, out sourcePositions);

                            for (int imageIndex = 0; imageIndex < imgs.Count; imageIndex++)
                            {
                                WriteableBitmap img = new WriteableBitmap(imgs[imageIndex] as RenderTargetBitmap);

                                // Map all pixels relative to source at (0,0)
                                Point offset = new Point(-sourcePositions[imageIndex].X, -sourcePositions[imageIndex].Y);
                                
                                int width = img.PixelWidth;
                                uint* backBufferPtr = (uint*)img.BackBuffer.ToPointer();
                                int stride = img.BackBufferStride / 4;

                                for (int y = 0; y < img.PixelHeight; y++)
                                {
                                    for (int x = 0; x < width; x++)
                                    {

                                        uint grayLevel = 255 - ((*(backBufferPtr + y * stride + x) & 0x00FF0000) >> 16);

                                        // Root location
                                        if (grayLevel > 0)
                                        {
                                            // Adjust bounding box
                                            int posX = x + (int)offset.X;
                                            int posY = y + (int)offset.Y;

                                            if (posX < left)
                                            {
                                                left = posX;
                                            }

                                            if (posY < top)
                                            {
                                                top = posY;
                                            }

                                            if (posX > right)
                                            {
                                                right = posX;
                                            }

                                            if (posY > bottom)
                                            {
                                                bottom = posY;
                                            }

                                            // Add to heat list
                                            grid.Accumulate(posX, posY, grayLevel);
                                        }
                                    }
                                }
                            }
                        }
                        catch (OutOfMemoryException)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            i--;
                        }
                    }

                    // Adjust bounding box back to 0,0
                    int finalOffsetX = -left;
                    int finalOffsetY = -top;
                    int finalWidth = (right - left);
                    int finalHeight = (bottom - top);

                    // Scale value
                    double heatMax = grid.Max;

                    WriteableBitmap heatImage = new WriteableBitmap(finalWidth, finalHeight, 96.0, 96.0, PixelFormats.Gray16, null);
                    heatImage.Lock();

                    ushort* ptr = (ushort*)heatImage.BackBuffer.ToPointer();
                    int grayStride = heatImage.BackBufferStride / 2;

                    for (int y = 0; y < finalHeight; y++)
                    {
                        for (int x = 0; x < finalWidth; x++)
                        {
                            double heat = grid[x - (int)finalOffsetX, y - (int)finalOffsetY];
                            ushort intensity = (ushort)((heat / heatMax) * ushort.MaxValue);
                            *(ptr + (y * grayStride + x)) = intensity;
                        }
                    }

                    heatImage.AddDirtyRect(new Int32Rect(0, 0, finalWidth, finalHeight));
                    heatImage.Unlock();


                    RootNav.Data.ImageEncoder.SaveImage(path + "\\heatMap.png", heatImage, RootNav.Data.ImageEncoder.EncodingType.PNG);

                    TimeSpan ts = DateTime.Now - dt;

                    break;
                case 2: // Single images
                    d = new System.Windows.Forms.FolderBrowserDialog();
                    result = d.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        path = d.SelectedPath;
                    }
                    else
                    {
                        return;
                    }

                    for (int i = 0; i < this.plantTags.Count; i++)
                    {
                        String tag = this.plantTags[i];

                        try
                        {
                            Tuple<SceneMetadata, SceneInfo, byte[]> result2 = rootReader.Read(tag, false);
                            SceneMetadata metadata = result2.Item1;
                            SceneInfo scene = result2.Item2;
                            List<PlantInfo> currentPlants = scene.Plants;
                            
                            Point sourcePosition;

                            RenderTargetBitmap img = CreatePlantImage(currentPlants, out sourcePosition, Brushes.Black, Brushes.White) as RenderTargetBitmap;

                            string file = path + "\\" + tag + ".png";

                            if (img != null)
                            {
                                RootNav.Data.ImageEncoder.SaveImage(file, new WriteableBitmap(img), RootNav.Data.ImageEncoder.EncodingType.PNG);
                            }

                            StreamWriter strm = new StreamWriter(path + "\\" + tag + ".txt", false);
                            strm.Write("Position: {0},{1}", sourcePosition.X, sourcePosition.Y);
                            strm.Close();
                        }
                        catch (OutOfMemoryException)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            i--;
                        }
                    }
                    break;
                default:
                    return;
            }

        }

        #endregion

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Handle closing of DB or files.
            Application.Current.Shutdown(0);
        }

        private void changeSourceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataSourceWindow dsw = new DataSourceWindow();
            bool? result = dsw.ShowDialog();
            if ((bool)result)
            {
                connectionInfo = dsw.ConnectionInfo;
                InitialiseConnection();
            }
        }

        #region Database Integrity Code
        private void databaseIntegrityMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Scan database for issues
            if (this.rootReader.GetType() != typeof(MySQLRootReader) || !this.rootReader.Connected)
            {
                MessageBox.Show("Not currently connected to a database", "Database Integrity");
                return;
            }
            else
            {
                List<string> allTags;
                if (this.unfilteredPlantTags != null && this.unfilteredPlantTags.Count > this.plantTags.Count)
                {
                    allTags = this.unfilteredPlantTags;
                }
                else
                {
                    allTags = this.plantTags;
                }

                integrityWorker = new BackgroundWorker();
                integrityWorker.WorkerReportsProgress = true;
                integrityWorker.WorkerSupportsCancellation = true;
                integrityWorker.DoWork += integrityCheck_DoWork;
                integrityWorker.ProgressChanged += integrityCheck_ProgressChanged;
                integrityWorker.RunWorkerCompleted += integrityCheck_RunWorkerCompleted;

                this.integrityProgressWindow = new DatabaseProgressWindow() { Task = "Analysing database", CanCancel = true };
                this.integrityProgressWindow.Closing += integrityCheck_Closing;
                this.integrityProgressWindow.Show();

                integrityWorker.RunWorkerAsync(allTags);
            }
        }

        void integrityCheck_Closing(object sender, CancelEventArgs e)
        {
            if (integrityWorker != null && integrityWorker.IsBusy)
            {
                integrityWorker.CancelAsync();
            }
        }

        BackgroundWorker integrityWorker = null;
        DatabaseProgressWindow integrityProgressWindow = null;

        void integrityCheck_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                integrityProgressWindow = null;
                integrityWorker = null;
            }
            else
            {
                // Close progress window
                integrityProgressWindow.Close();

                object[] obj = e.Result as object[];
                if (obj != null)
                {
                    // Handle lists of integrity violations
                    List<Tuple<string, DateTime>> duplicateTags = obj[0] as List<Tuple<string, DateTime>>;
                    List<Tuple<string, DateTime, string>> reversedLaterals = obj[1] as List<Tuple<string, DateTime, string>>;

                    if (duplicateTags.Count > 0 || reversedLaterals.Count > 0)
                    {
                        BackgroundWorker bw = new BackgroundWorker();
                        bw.WorkerReportsProgress = false;
                        bw.WorkerSupportsCancellation = false;
                        bw.DoWork += bw_DoWork;
                        bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                        bw.RunWorkerAsync(obj);
                    }
                    else
                    {
                        MessageBox.Show("Integrity checks completed, no issues found.", "Database Integrity");
                    }
                }

                this.integrityProgressWindow = null;
                this.integrityWorker = null;
            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Integrity checks completed", "Database Integrity");
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // Handle lists of integrity violations
            List<Tuple<string, DateTime>> duplicateTags = (e.Argument as object[])[0] as List<Tuple<string, DateTime>>;
            List<Tuple<string, DateTime, string>> reversedLaterals = (e.Argument as object[])[1] as List<Tuple<string, DateTime, string>>;

            // Duplicate tags
            if (duplicateTags.Count > 0)
            {
                string message = string.Format("{0} duplicate tags have been found in the database. These occur where the same tag has been used on two separate reconstructions, but at different times. This tool can remove all but the oldest architecture, would you like to do this now?", duplicateTags.Count);
                MessageBoxResult mbResult = MessageBox.Show(message, "Duplicated Tags", MessageBoxButton.YesNo);

                if (mbResult == MessageBoxResult.Yes)
                {
                    // DELETE DUPLICATE TAGS
                    duplicateDeleteWorker = new BackgroundWorker();
                    duplicateDeleteWorker.WorkerReportsProgress = true;
                    duplicateDeleteWorker.DoWork += duplicateDeleteWorker_DoWork;
                    duplicateDeleteWorker.RunWorkerCompleted += duplicateDeleteWorker_RunWorkerCompleted;
                    duplicateDeleteWorker.ProgressChanged += duplicateDeleteWorker_ProgressChanged;

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        integrityProgressWindow = new DatabaseProgressWindow() { Task = "Deleting duplicate entries", CanCancel = false };
                        integrityProgressWindow.Show();
                    }));
                  
                    duplicateDeleteWorkerWaitEvent.Reset();
                    duplicateDeleteWorker.RunWorkerAsync(duplicateTags);
                    duplicateDeleteWorkerWaitEvent.WaitOne();

                    integrityProgressWindow.Dispatcher.Invoke(new Action(() =>
                    {
                        integrityProgressWindow.Hide();
                        integrityProgressWindow = null;
                    }));
                }
            }

            // Reversed lateral roots
            if (reversedLaterals.Count > 0)
            {
                string message = "Numerous reversed laterals have been found in the database. These were caused by a rare bug present in RootNav versions prior to 1.8. This tool can rectify this issue for all laterals, would you like to do this now?";
                MessageBoxResult mbResult = MessageBox.Show(message, "Reversed Laterals", MessageBoxButton.YesNo);

                if (mbResult == MessageBoxResult.Yes)
                {
                    // DELETE DUPLICATE TAGS
                    reverseLateralWorker = new BackgroundWorker();
                    reverseLateralWorker.WorkerReportsProgress = true;
                    reverseLateralWorker.DoWork += reverseLateralWorker_DoWork;
                    reverseLateralWorker.RunWorkerCompleted += reverseLateralWorker_RunWorkerCompleted;
                    reverseLateralWorker.ProgressChanged += reverseLateralWorker_ProgressChanged;

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        integrityProgressWindow = new DatabaseProgressWindow() { Task = "Reversing incorrect laterals", CanCancel = false };
                        integrityProgressWindow.Show();
                    }));

                    reverseLateralWorkerWaitEvent.Reset();
                    reverseLateralWorker.RunWorkerAsync(reversedLaterals);
                    reverseLateralWorkerWaitEvent.WaitOne();

                    integrityProgressWindow.Dispatcher.Invoke(new Action(() =>
                    {
                        integrityProgressWindow.Hide();
                        integrityProgressWindow = null;
                    }));
                }
            }
        }

        void reverseLateralWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var state = e.UserState as Tuple<int, int>;

            if (state != null)
            {
                integrityProgressWindow.Dispatcher.BeginInvoke(new Action(() => { integrityProgressWindow.SetProgress(state.Item1, state.Item2); }));
            }
        }

        void reverseLateralWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            reverseLateralWorkerWaitEvent.Set();
        }

        void reverseLateralWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            List<Tuple<string, DateTime, string>> reversedLaterals = e.Argument as List<Tuple<string, DateTime, string>>;
            int count = 0;
            int total = reversedLaterals.Count;
            DatabaseManager dbm = new MySQLDatabaseManager();
            dbm.Open(this.connectionInfo.ToMySQLConnectionString());

            double angleMax = 30.0, angleMin = 10.0, parentAngleRadius = 20.0, tipAngleDistance = 40.0;

            using (MySqlConnection connection = dbm.Connection as MySqlConnection)
            {
                foreach (var reversedLateral in reversedLaterals)
                {
                    string tag = reversedLateral.Item1;
                    DateTime stamp = reversedLateral.Item2;
                    string lateralID = reversedLateral.Item3;
                    string parentID = lateralID.Substring(0,lateralID.LastIndexOf('.'));

                    // TODO: Fix reversed lateral
                    // Obtain reversed root to be fixed
                    MySqlCommand selectCommand = new MySqlCommand("SELECT (SELECT Spline FROM rootdata WHERE Tag = (?tag) AND Stamp = (?Stamp) AND RelativeID = (?pid)) AS PrimarySpline, (SELECT Spline FROM rootdata WHERE Tag = (?tag) AND Stamp = (?Stamp) AND RelativeID = (?lid)) AS LateralSpline", connection);
                    selectCommand.Parameters.AddWithValue("?tag", tag);
                    selectCommand.Parameters.AddWithValue("?stamp", stamp);
                    selectCommand.Parameters.AddWithValue("?lid", lateralID);
                    selectCommand.Parameters.AddWithValue("?pid", parentID);

                    SampledSpline primarySpline = null;
                    SampledSpline lateralSpline = null;

                    using (MySqlDataReader Reader = selectCommand.ExecuteReader())
                    {
                        if (Reader.Read())
                        {
                            // Primary Spline
                            if (!Reader.IsDBNull(0))
                            {
                                try
                                {
                                    primarySpline = RootNav.Data.SplineSerializer.BinaryToObject((byte[])Reader["PrimarySpline"]) as SampledSpline;
                                }
                                catch
                                {
                                    primarySpline = null;
                                }
                            }

                            // Lateral Spline
                            if (!Reader.IsDBNull(1))
                            {
                                try
                                {
                                    lateralSpline = RootNav.Data.SplineSerializer.BinaryToObject((byte[])Reader["LateralSpline"]) as SampledSpline;
                                }
                                catch
                                {
                                    lateralSpline = null;
                                }
                            }
                        }
                    }
                    
                    // If we have valid lateral and primary roots
                    if (primarySpline != null && lateralSpline != null)
                    {
                        // Flip spline
                        lateralSpline.Reverse();
                                
                        Point start = lateralSpline.Start;
                        Point end = lateralSpline.End;

                        SplinePositionReference startReference = primarySpline.GetPositionReference(start);

                        double startLength = primarySpline.GetLength(startReference);

                        string startString = ToRoundString(start);
                        string endString = ToRoundString(end);

                        Point vectorStart, vectorEnd;


                        // Emergence Angle
                        if (angleMax > lateralSpline.Length)
                        {
                            double newMinDistanceOffset = angleMax - lateralSpline.Length;
                            vectorStart = lateralSpline.GetPoint(lateralSpline.GetPositionReference(Math.Max(0.0, angleMin - newMinDistanceOffset)));
                            vectorEnd = lateralSpline.GetPoint(lateralSpline.GetPositionReference(lateralSpline.Length));
                        }
                        else
                        {
                            vectorStart = lateralSpline.GetPoint(lateralSpline.GetPositionReference(Math.Min(lateralSpline.Length, angleMin)));
                            vectorEnd = lateralSpline.GetPoint(lateralSpline.GetPositionReference(angleMax));
                        }

                        Vector rootVector = vectorEnd - vectorStart;

                        Point parentStart = primarySpline.GetPoint(primarySpline.GetPositionReference(Math.Max(0.0, startLength - parentAngleRadius)));
                        Point parentEnd = primarySpline.GetPoint(primarySpline.GetPositionReference(Math.Min(primarySpline.Length, startLength + parentAngleRadius)));

                        Vector parentVector = parentEnd - parentStart;

                        double emergenceAngle = Math.Round(Vector.AngleBetween(rootVector, parentVector), 1);

                        double tipDistance = lateralSpline.SampledPointsLengths.Last();
                        double innerDistance = Math.Max(0, tipDistance - tipAngleDistance);

                        Point innerPoint = lateralSpline.GetPoint(lateralSpline.GetPositionReference(innerDistance));
                        Point tipPoint = lateralSpline.GetPoint(lateralSpline.GetPositionReference(tipDistance));

                        // Tip Angle is angle between horizontal vector and the tip vector
                        double angle = 90 - Vector.AngleBetween(new Vector(1, 0), tipPoint - innerPoint);
                        double tipAngle = Math.Round(angle > 180 ? angle - 360 : angle, 1);

                        string command = "UPDATE rootdata SET Start = (?start), End = (?end), " +
                            "EmergenceAngle = (?ea), TipAngle = (?ta), StartReference = (?sr), StartDistance = (?sd), Spline = (?spline) " +
                            "WHERE Tag = (?tag) AND Stamp = (?Stamp) AND RelativeID = (?lid)";

                        MySqlCommand updateCommand = new MySqlCommand(command, connection);
                        updateCommand.Parameters.AddWithValue("?start", startString);
                        updateCommand.Parameters.AddWithValue("?end", endString);
                        updateCommand.Parameters.AddWithValue("?ea", emergenceAngle);
                        updateCommand.Parameters.AddWithValue("?ta", tipAngle);
                        updateCommand.Parameters.AddWithValue("?sr", SplineSerializer.ObjectToBinary(startReference));
                        updateCommand.Parameters.AddWithValue("?sd", Math.Round(startLength, 2));
                        updateCommand.Parameters.AddWithValue("?spline", SplineSerializer.ObjectToBinary(lateralSpline));
                        updateCommand.Parameters.AddWithValue("?tag", tag);
                        updateCommand.Parameters.AddWithValue("?stamp", stamp);
                        updateCommand.Parameters.AddWithValue("?lid", lateralID);

                        int result = updateCommand.ExecuteNonQuery();
        
                    }

                    count++;

                    // Check for cancellation
                    if (bw.CancellationPending)
                    {
                        e.Cancel = true;
                        e.Result = null;
                        return;
                    }

                    // Report progress
                    bw.ReportProgress((count / total) * 100, new Tuple<int, int>(count, total));
                }
            }
            dbm.Close();

            return;
        }

        private static String ToRoundString(System.Windows.Point p)
        {
            return Math.Round(p.X).ToString() + "," + Math.Round(p.Y).ToString();
        }

        BackgroundWorker duplicateDeleteWorker;
        BackgroundWorker reverseLateralWorker;

        System.Threading.ManualResetEvent duplicateDeleteWorkerWaitEvent = new System.Threading.ManualResetEvent(false);
        System.Threading.ManualResetEvent reverseLateralWorkerWaitEvent = new System.Threading.ManualResetEvent(false);

        void duplicateDeleteWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var state = e.UserState as Tuple<int, int>;

            if (state != null)
            {
                integrityProgressWindow.Dispatcher.BeginInvoke(new Action(() => { integrityProgressWindow.SetProgress(state.Item1, state.Item2); }));
            }
        }

        void duplicateDeleteWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            duplicateDeleteWorkerWaitEvent.Set();
        }

        void duplicateDeleteWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            List<Tuple<string, DateTime>> duplicateTags = e.Argument as List<Tuple<string, DateTime>>;
            int count = 0;
            int total = duplicateTags.Count;
 
            DatabaseManager dbm = new MySQLDatabaseManager();
            dbm.Open(this.connectionInfo.ToMySQLConnectionString());

            using (MySqlConnection connection = dbm.Connection as MySqlConnection)
            {
                foreach (var duplicateTag in duplicateTags)
                {
                    string tag = duplicateTag.Item1;
                    DateTime stamp = duplicateTag.Item2;

                    // Delete duplicated tagged entries
                    MySqlCommand deleteCommand = new MySqlCommand("DELETE FROM rootdata WHERE Tag = (?tag) AND Stamp = (?stamp)", connection);
                    deleteCommand.Parameters.AddWithValue("?tag", tag);
                    deleteCommand.Parameters.AddWithValue("?stamp", stamp);

                    // DELETE COMMAND
                    int result = deleteCommand.ExecuteNonQuery();

                    count++;

                    // Check for cancellation
                    if (bw.CancellationPending)
                    {
                        e.Cancel = true;
                        e.Result = null;
                        return;
                    }

                    // Report progress
                    bw.ReportProgress((count / total) * 100, new Tuple<int, int>(count, total));
                }
            }
            dbm.Close();

            return;
        }

        void integrityCheck_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var state = e.UserState as Tuple<int, int>;

            if (state != null)
            {
                integrityProgressWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (integrityProgressWindow != null)
                            integrityProgressWindow.SetProgress(state.Item1, state.Item2);
                    }
                    catch
                    {
                        return;
                    }
                }));
            }
        }

        void integrityCheck_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            List<string> allTags = e.Argument as List<string>;
            
            if (allTags == null || bw == null)
                return;

            int count = 0;
            int total = allTags.Count;

            // Storage for duplicate plants (Tag, Stamp)
            List<Tuple<string, DateTime>> duplicateTaggedPlants = new List<Tuple<string, DateTime>>();

            // Storage for reversed lateral roots (Tag, Stamp, RelativeID)
            List<Tuple<string, DateTime, string>> reversedLateralRoots = new List<Tuple<string, DateTime, string>>();

            foreach (string tag in allTags)
            {
                Tuple<SceneMetadata, SceneInfo, byte[]> result2 = rootReader.Read(tag, false);
                SceneMetadata metadata = result2.Item1;
                SceneInfo scene = result2.Item2;
                List<PlantInfo> plants = scene.Plants;

                // Check for duplicate tags with different time stamps
                HashSet<DateTime> stamps = new HashSet<DateTime>();
                foreach (PlantInfo plant in plants)
                {
                    stamps.Add(plant.Stamp);
                }

                if (stamps.Count > 1)
                {
                    // Duplicate plant found
                    DateTime minimum = stamps.Min();
                    foreach (DateTime stamp in stamps)
                    {
                        if (stamp != minimum)
                        {
                            duplicateTaggedPlants.Add(new Tuple<string, DateTime>(tag, stamp));
                        }
                    }
                }

                foreach (PlantInfo plant in plants)
                {
                    // For each primary root
                    foreach (RootInfo primary in plant)
                    {
                        if (primary.Children != null && primary.Spline != null)
                        {
                            // For each lateral
                            foreach (RootInfo lateral in primary.Children)
                            {
                                // Test for reversal
                                Point start = lateral.Spline.Start;
                                Point end = lateral.Spline.End;

                                // Calculate the nearest position of the start and end to the primary parent
                                Point startOnPrimary = primary.Spline.GetPoint(primary.Spline.GetPositionReference(start));
                                Point endOnPrimary = primary.Spline.GetPoint(primary.Spline.GetPositionReference(end));

                                // Start should be closer to parent, otherwise it's been reversed
                                if (PointDistance(start, startOnPrimary) > PointDistance(end, endOnPrimary))
                                {
                                    reversedLateralRoots.Add(new Tuple<string, DateTime, string>(tag, lateral.Stamp, lateral.RelativeID));
                                }
                            }
                        }
                    }
                }

                count++;

                // Check for cancellation
                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                    e.Result = null;
                    return;
                }

                // Report progress
                bw.ReportProgress((count / total) * 100, new Tuple<int, int>(count, total));
            }

            // Return results
            object[] result = new object[] { duplicateTaggedPlants, reversedLateralRoots };
            e.Result = result;
        }

        #endregion

        private double PointDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2.0) + Math.Pow(a.Y - b.Y, 2.0));
        }
    }


    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class NotVisibilityConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class VisibilityConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    public class CountConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return (int)values[0] > 0 || (int)values[1] > 0 ? false : true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(Brush))]
    public class EllipseBrushConverter : DependencyObject, IValueConverter
    {
        RadialGradientBrush red;
        RadialGradientBrush green;

        public EllipseBrushConverter()
        {
            red = new RadialGradientBrush();
            red.RadiusX = 0.5;
            red.RadiusY = 0.5;
            red.Center = new Point(0.5, 0.5);
            red.GradientOrigin = new Point(0.5, 0.25);
            red.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFAAAA"), 0.0));
            red.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("Red"), 1.0));

            green = new RadialGradientBrush();
            green.RadiusX = 0.5;
            green.RadiusY = 0.5;
            green.Center = new Point(0.5, 0.5);
            green.GradientOrigin = new Point(0.5, 0.25);
            green.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFAAFFAA"), 0.0));
            green.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("Green"), 1.0));

        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? green : red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

}
