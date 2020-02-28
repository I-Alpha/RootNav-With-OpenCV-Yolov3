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
using System.IO;
using System.Windows.Media.Animation;
using System.Data;
using System.Xml;

using RootNav.Core;
using RootNav.Core.MixtureModels;
using RootNav.Interface.Controls;
using RootNav.Core.LiveWires;
using RootNav.Core.DataStructures;
using RootNav.Core.Imaging;
using RootNav.IO;
using System.ComponentModel;
using RootNav.Core.Tips;
using RootNav.Core.Measurement;
using RootNav.Measurement;
using RootNav.Data;
using RootNav.Data.IO;
using RootNav.Data.IO.Databases;
using OpenCvSharpYolo3; 

namespace RootNav.Interface.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static double BackgroundPenalty = 0.1;

        public delegate void StatusTextUpdateDelegate(String s);
        public delegate void ScreenImageUpdateDelegate(WriteableBitmap wbmp);
        public delegate void EMCompletedDelegate();
        public delegate void LiveWireLateralCompletedDelegate(List<LiveWireLateralPath> paths);
        public delegate void LiveWirePrimaryCompletedDelegate(List<LiveWirePrimaryPath> paths);
        public delegate void LiveWireReCompletedDelegate();
        
        private EMManager emManager = null;
        private byte[] intensityBuffer = null;
        private GaussianMixtureModel highlightedMixture = null;
        private LiveWirePrimaryManager primaryLiveWireManager = null;
        private LiveWireLateralManager lateralLiveWireManager = null;

        private EMConfiguration[] configurations;
        private EMConfiguration customConfiguration = null;
        private int currentEMConfiguration = 0;
        
        private bool connectionExists = false;

        RootNav.Data.IO.Databases.DatabaseManager databaseManager = null;
        private SceneMetadata.ImageInfo imageInfo = null;
        private WriteableBitmap featureBitmap = null;
        private WriteableBitmap sourceBitmap = null;
        private WriteableBitmap probabilityBitmap = null;
        private bool loadImageZoomOnce = false;

        private double[] probabilityMapBestClass = null;
        private double[] probabilityMapBrightestClass = null;
        private double[] probabilityMapSecondClass = null;
        private double[,] distanceProbabilityMap = null;

        private string FileName { get; set; }

        public bool showSourceImage = false;

        private RootDetectionScreenOverlay screenOverlay = null;

        private LiveWireGraph currentGraph = null;

        private TiffHeaderInfo imageHeaderInfo = null;

        private List<LiveWireWeightDescriptor> baseWeightDescriptors = null;

        public List<LiveWireWeightDescriptor> BaseWeightDescriptors
        {
            get { return baseWeightDescriptors; }
            set { baseWeightDescriptors = value; }
        }

        private RootTerminalCollection terminalCollection = new RootTerminalCollection();

        public MainWindow()
        {
            InitializeComponent();
            InitializeAdorners();

            if (System.Reflection.Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture.ToString().Contains("64"))
            {
                this.Title += " (64-bit)";
            }

            connectionInfo = ConnectionParams.FromEncryptedStorage();
            if (connectionInfo != null && OpenOutputSourceConnection(false))
            {
                if (connectionInfo.Source == ConnectionSource.MySQLDatabase)
                {
                    this.measurementToolbox.SetConnected("MySQL Database", connectionInfo.Server);
                }
                else
                {
                    this.measurementToolbox.SetConnected("RSML Directory", connectionInfo.Directory);
                }
            }
            else
            {
                this.measurementToolbox.SetUnconnected();
            }

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.detectionToolbox.snapToTipsCheckBox.Checked += new RoutedEventHandler(snapToTipsCheckBox_Checked);
            this.detectionToolbox.snapToTipsCheckBox.Unchecked += new RoutedEventHandler(snapToTipsCheckBox_Unchecked);
        }

        void snapToTipsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.screenOverlay.SnapToTip = false;
        }

        void snapToTipsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.screenOverlay.SnapToTip = true;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.configurations = EMConfiguration.LoadFromXML();
            }
            catch
            {
                MessageBox.Show("An invalid value has been found in the E-M configuration XML file. Please correct this before running RootNav.", "Configuration XML Error");
                Application.Current.Shutdown();
                return;
            }

            this.currentEMConfiguration = EMConfiguration.DefaultIndex(configurations);

            if (this.configurations.Length > 0)
            {
                this.EMDetectionToolbox.LoadPresetNames((from config in this.configurations select config.Name).ToArray(), currentEMConfiguration);
                return;
            }

            

        }

        public void EMToolboxConfigurationChanged(int newindex)
        {
            if (newindex >= 0 && newindex < this.configurations.Length)
            {
                this.currentEMConfiguration = newindex;
                this.EMDetectionToolbox.FromEMConfiguration(configurations[currentEMConfiguration]);
            }
            
            
        }

        public void BeginEMFromToolbox()
        {
            this.EMDetectionToolbox.reCalculateEMButton.IsEnabled = false;
            this.DetectionPanel.IsEnabled = false;

            if (this.EMDetectionToolbox.emPresetComboBox.SelectedIndex >= this.configurations.Length)
            {
                try
                {
                    this.customConfiguration = this.EMDetectionToolbox.ToEMConfiguration();
                }
                catch
                {
                    // Error creating EM configuration
                    MessageBox.Show("An invalid value has been found in the E-M configuration. Please correct this then apply E-M again.", "Configuration Error");
                    this.EMDetectionToolbox.reCalculateEMButton.IsEnabled = true;
                    this.DetectionPanel.IsEnabled = true;
                    return;
                }
            }
            else
            {
                this.customConfiguration = null;
            }

            EMProcessing();
        }

        private void EMProcessing()
        {
            WriteableBitmap greybmp = Core.Imaging.ImageProcessor.MakeGreyscale32bpp(sourceBitmap);

            // Intensity Buffer
            intensityBuffer = RootNav.IO.ImageConverter.WriteableBitmapToIntensityBuffer(greybmp);

            int width = greybmp.PixelWidth;
            int height = greybmp.PixelHeight;

            // Initial custom E-M config?
            if (this.EMDetectionToolbox.emPresetComboBox.SelectedIndex >= this.configurations.Length)
            {
                try
                {
                    this.customConfiguration = this.EMDetectionToolbox.ToEMConfiguration();
                }
                catch
                {
                    // Error creating EM configuration
                    MessageBox.Show("An invalid value has been found in the E-M configuration. Please correct this then apply E-M again.", "Configuration Error");
                    this.customConfiguration = null;
                    return;
                }
            }
            else
            {
                this.customConfiguration = null;
            }


            this.emManager = new EMManager()
            {
                IntensityBuffer = intensityBuffer,
                Width = width,
                Height = height,
                Configuration = customConfiguration == null ? configurations[currentEMConfiguration] : customConfiguration,
                ThreadCount = Core.Threading.ThreadParams.EMThreadCount
            };

            int GMMArrayWidth = (int)Math.Ceiling(greybmp.PixelWidth / (double)emManager.Configuration.PatchSize);
            int GMMArrayHeight = (int)Math.Ceiling(greybmp.PixelHeight / (double)emManager.Configuration.PatchSize);

            GaussianMixtureModel[,] GMMArray = new GaussianMixtureModel[GMMArrayWidth, GMMArrayHeight];

            this.emManager.ProgressChanged += new ProgressChangedEventHandler(EMManagerProgressChanged);
            this.emManager.ProgressCompleted += new RunWorkerCompletedEventHandler(EMManagerProgressCompleted);

            this.UpdateStatusText("Status: Processing " + (GMMArrayHeight * GMMArrayWidth).ToString() + " patches");

            this.emManager.Run();

            this.StartScreenLabel.Visibility = System.Windows.Visibility.Hidden;

            this.screenOverlay.IsBusy = true;

            this.DetectionPanel.IsEnabled = false;
            this.preMeasurementToolbox.MeasurementButton.IsEnabled = false;

        }

        public void BeginMeasurementStage()
        {
            double conversion;
            bool conversionExists = double.TryParse(this.preMeasurementToolbox.imageResolutionTextbox.Text, out conversion);

            if (!conversionExists)
            {
                conversion = 0;
            }

            this.imageInfo.Resolution = conversion;
            this.imageInfo.Unit = conversion == 0 ? "pixels" : "mm";

            this.screenOverlay.InitialiseMeasurementStage((int)this.preMeasurementToolbox.spacingSlider.Value, conversion == 0 ? 0 : 1 / conversion);

            Binding b = new Binding();
            b.Source = this.screenOverlay.Roots.RootTree;
            BindingOperations.SetBinding(this.rootTreeView, TreeView.ItemsSourceProperty, b);

            this.rootTreeView.MouseMove += new MouseEventHandler(rootTreeView_MouseMove);
            this.rootTreeView.MouseLeave += new MouseEventHandler(rootTreeView_MouseLeave);
     
            this.rootTreeView.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(rootTreeView_SelectedItemChanged);

            this.detectionSlidePanel.BeginHide();
            this.measurementSlidePanel.BeginShow();
        }

        void rootTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.screenOverlay.InvalidateVisual();
        }

        void rootTreeView_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.screenOverlay.CurrentHighlightedRoot != null)
            {
                foreach (RootBase r in this.screenOverlay.Roots)
                {
                    r.IsHighlighted = false;
                }
                this.screenOverlay.CurrentHighlightedRoot = null;
                this.screenOverlay.InvalidateVisual();
            }
        }

        void rootTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            RootBase r = GetRootDataFromPoint(this.rootTreeView, e.GetPosition(this.rootTreeView));
            if (r != null)
            {
                if (this.screenOverlay.CurrentHighlightedRoot != r)
                {
                    this.screenOverlay.CurrentHighlightedRoot = r;
                    r.IsHighlighted = true;

                    foreach (RootBase other in this.screenOverlay.Roots)
                    {
                        if (other != r && other.IsHighlighted)
                        {
                            other.IsHighlighted = false;
                        }
                    }

                    this.screenOverlay.InvalidateVisual();
                }
            }
            else
            {
                if (this.screenOverlay.CurrentHighlightedRoot != null)
                {
                    this.screenOverlay.CurrentHighlightedRoot = null;

                    foreach (RootBase other in this.screenOverlay.Roots)
                    {
                        if (other.IsHighlighted)
                        {
                            other.IsHighlighted = false;
                        }
                    }

                    this.screenOverlay.InvalidateVisual();
                }


            }
        }

        private RootBase GetRootDataFromPoint(ItemsControl source, Point point)
        {
            var item = source.InputHitTest(point) as FrameworkElement;    
            return item == null ? null : item.DataContext as RootBase;
        }

        private void InitializeAdorners()
        {
            AdornerLayer AL = AdornerLayer.GetAdornerLayer(this.Screen);
            this.screenOverlay = new RootDetectionScreenOverlay(this.Screen, this.detectionToolbox, this.ScreenScrollViewer) { Visibility = System.Windows.Visibility.Hidden };
            AL.Add(this.screenOverlay);

            // Bind processing label to the overlay
            Binding binding = new Binding();
            binding.Source = this.screenOverlay;
            binding.Path = new PropertyPath("IsBusy");
            binding.Converter = new IsBusyVisibleConverter();
            BindingOperations.SetBinding(this.ProcessingLabel, Label.VisibilityProperty, binding);
        }

        private static Random rand = new Random();
        private static void Grand(out double num1, out double num2)
        {
            double x1, x2, w;
            do
            {
                x1 = 2.0 * rand.NextDouble() - 1.0;
                x2 = 2.0 * rand.NextDouble() - 1.0;
                w = x1 * x1 + x2 * x2;
            } while (w >= 1.0);

            w = Math.Sqrt((-2.0 * Math.Log(w)) / w);
            num1 = x1 * w;
            num2 = x2 * w;
        }

        public bool FileDragInProgress { get; set; }

        protected override void OnDragEnter(DragEventArgs e)
        {
            FileDragInProgress = true;
            base.OnDragEnter(e);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            FileDragInProgress = false;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            { 
                string[] droppedFilePaths = 
                e.Data.GetData(DataFormats.FileDrop, true) as string[];

                if (droppedFilePaths.Length > 0)
                {
                    if (VerifyImage(droppedFilePaths.First()))
                    {
                        ResetAll();
                        LoadImages(droppedFilePaths.First());
                    }
                    else
                    {
                        if (this.sourceBitmap == null)
                        {
                            this.StartScreenLabel.Visibility = System.Windows.Visibility.Visible;
                        }
                    }
                }
            }
        }

        public static MainWindow GetMainWindowParent(FrameworkElement element)
        {
            DependencyObject dpParent = element.Parent;
            DependencyObject nextdpParent = null;

            if (dpParent == null)
                return null;
            do
            {
                nextdpParent = LogicalTreeHelper.GetParent(dpParent);
                if (nextdpParent == null)
                    nextdpParent = VisualTreeHelper.GetParent(dpParent);

                if (nextdpParent == null)
                    return null;
                else
                    dpParent = nextdpParent;


            } while (dpParent.GetType().BaseType != typeof(Window));
            return dpParent as MainWindow;
        }

        private bool VerifyImage(string filePath)
        {
            try
            {
                BitmapImage bmp = new BitmapImage();

                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                }
                bmp.Freeze();

                
                WriteableBitmap wbmp = new WriteableBitmap(bmp);

                if (wbmp.Format == PixelFormats.Indexed8 || wbmp.Format == PixelFormats.Bgra32 || wbmp.Format == PixelFormats.Gray8)
                    wbmp = IO.ImageConverter.ConvertTo32bpp(wbmp);

                if (wbmp.PixelWidth * wbmp.PixelHeight > 7000000) // 7MP images will likely cause out of memory exceptions. They are also unnecessarily large for root detection
                {
                    MessageBox.Show("Images of over 7 megapixels are too large to be processed efficiently. Images around 5 megapixels are recommended.", "Image too large.");
                    this.imageInfo = null;
                    return false;
                }

                return true;
                
            }
            catch
            {
                MessageBox.Show("Please select a valid image file");
                return false;
            }
        }

        private void LoadImages(params string[] filePaths)
        {
            try
            {
                String filePath = filePaths[0];
                this.FileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                this.imageInfo = new SceneMetadata.ImageInfo()
                {
                    Label = filePath,
                    Hash = Hashing.Sha256(filePath),
                    Background = "dark",
                    Unit = "pixels",
                    TimeInSequence = 0.0
                };



                // For TIF files, extract header information
                if (System.IO.Path.GetExtension(filePath) == ".tif"
                    || System.IO.Path.GetExtension(filePath) == ".tiff")
                {
                    imageHeaderInfo = TiffHeaderDecoder.ReadHeaderInfo(filePath);
                }

                BitmapImage bmp = new BitmapImage();

                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                }
                bmp.Freeze();

                WriteableBitmap wbmp = new WriteableBitmap(bmp);

                imageInfo.Dpi = wbmp.DpiX;

                if (wbmp.Format == PixelFormats.Indexed8 || wbmp.Format == PixelFormats.Bgra32 || wbmp.Format == PixelFormats.Gray8)
                    wbmp = IO.ImageConverter.ConvertTo32bpp(wbmp);

                if (wbmp.PixelWidth * wbmp.PixelHeight > 7000000) // 7MP images will likely cause out of memory exceptions. They are also unnecessarily large for root detection
                {
                    MessageBox.Show("Images of over 7 megapixels are too large to be processed efficiently. Images around 5 megapixels are recommended.", "Image too large.");
                    this.StartScreenLabel.Visibility = System.Windows.Visibility.Visible;
                    this.imageInfo = null;
                    return;
                }

                sourceBitmap = wbmp;
             
            }

            catch
            {
                this.StartScreenLabel.Visibility = System.Windows.Visibility.Visible;
                MessageBox.Show("Please select a valid image file");
            }

            EMProcessing();
        }

        private void TaskProgressChanged(int progress)
        {
            this.mainProgressBar.Value = progress;
        }

        private void EMManagerProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            this.Dispatcher.Invoke(new TaskProgressChangedHandler(TaskProgressChanged), new object[] { args.ProgressPercentage });
        }

        private void LiveWireManagerProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            this.Dispatcher.Invoke(new TaskProgressChangedHandler(TaskProgressChanged), new object[] { args.ProgressPercentage });
        }

        private void EMTaskCompleted()
        {
            this.ImageMenu.IsEnabled = true;
            this.EMDetectionToolbox.reCalculateEMButton.IsEnabled = true;
            this.DetectionPanel.IsEnabled = true;
            this.PreMeasurementPanel.IsEnabled = true;
            this.screenOverlay.Visibility = Visibility.Visible;
            this.ScreenScrollViewer.Focus();
        }

        private void UpdateStatusText(String s)
        {
            this.statusText.Text = s;
        }

        private void LiveWireManagerReProgressCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            // Weightings
            this.baseWeightDescriptors.Clear();
            foreach (LiveWirePath path in this.screenOverlay.Paths)
            {
                LiveWirePrimaryPath primary = path as LiveWirePrimaryPath;
                if (primary != null)
                {
                    this.baseWeightDescriptors.Add(new LiveWireWeightDescriptor(primary, probabilityMapBestClass, this.emManager.Width, this.emManager.Height));
                }
            }

            // UI
            this.Dispatcher.BeginInvoke(new LiveWireReCompletedDelegate(this.LiveWireReWorkCompletedUI));
        }

        private void LiveWireReWorkCompletedUI()
        {
            this.screenOverlay.RecalculateAllSamples();
            this.screenOverlay.InvalidateVisual();
            this.screenOverlay.IsBusy = false;
            this.statusText.Text = "Status: Idle";
        }

        private void LiveWireManagerProgressCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            LiveWireManager manager = sender as LiveWireManager;

            if (manager == null)
            {
                return;
            }

            if (sender == this.primaryLiveWireManager)
            {
                if (this.baseWeightDescriptors != null)
                {
                    this.baseWeightDescriptors.Clear();
                }

                List<LiveWirePrimaryPath> paths = new List<LiveWirePrimaryPath>();
                foreach (KeyValuePair<Tuple<int, int>, List<Point>> pointPath in this.primaryLiveWireManager.Paths)
                {
                    paths.Add(new LiveWirePrimaryPath(pointPath.Key.Item1, pointPath.Key.Item2, pointPath.Value, this.primaryLiveWireManager.ControlPointIndices[pointPath.Key]));
                }

                // Weightings
                this.baseWeightDescriptors = new List<LiveWireWeightDescriptor>();
                foreach (LiveWirePrimaryPath pointPath in paths)
                {
                    this.baseWeightDescriptors.Add(new LiveWireWeightDescriptor(pointPath, probabilityMapBestClass, this.emManager.Width, this.emManager.Height));
                }

                // UI
                this.Dispatcher.BeginInvoke(new LiveWirePrimaryCompletedDelegate(this.LiveWirePrimaryWorkCompletedUI), paths);
            }

            if (sender == this.lateralLiveWireManager)
            {
                List<LiveWireLateralPath> paths = new List<LiveWireLateralPath>();
                foreach (var item in this.lateralLiveWireManager.Paths)
                {
                    List<Point> path = item.Value;
                    TargetPathPoint target = this.lateralLiveWireManager.TargetPathIndices[item.Key];
                    List<int> indices = this.lateralLiveWireManager.ControlPointIndices[item.Key];
                    paths.Add(new LiveWireLateralPath(item.Key, target, path, indices));
                }

                // UI
                this.Dispatcher.BeginInvoke(new LiveWireLateralCompletedDelegate(this.LiveWireLateralWorkCompletedUI), paths);
            }
        }

        private void LiveWirePrimaryWorkCompletedUI(List<LiveWirePrimaryPath> paths)
        {
            List<LiveWirePrimaryPath> basePaths;

            LiveWireRootAssociation.FindRoots(this.screenOverlay.Terminals,
                                              paths,
                                              this.baseWeightDescriptors,
                                              out basePaths,
                                              out this.baseWeightDescriptors);

            // Create gray image the first time only
            if (this.screenOverlay.Paths.Count == 0)
            {
                WriteableBitmap gray = RootNav.IO.ImageConverter.ConvertToGrayScaleUniform(this.ScreenImage.ImageSource as WriteableBitmap);
                this.Dispatcher.Invoke(new ScreenImageUpdateDelegate(this.UpdateScreenImage), gray);
            }
            else
            {
                // Clear old paths
                this.screenOverlay.ClearAll();
            }

            foreach (LiveWirePrimaryPath path in basePaths)
            {
                this.screenOverlay.Paths.Add(path);
            }
            this.detectionToolbox.UncheckToggleButtons(null);
            this.screenOverlay.IsBusy = false;
            this.statusText.Text = "Status: Idle";
            this.preMeasurementToolbox.MeasurementButton.IsEnabled = true;
        }

        private void LiveWireLateralWorkCompletedUI(List<LiveWireLateralPath> paths)
        {
            if (paths.Count > 0)
            {
                if (this.screenOverlay.Paths.Laterals.Count() > 0)
                {
                    this.screenOverlay.ClearLaterals();
                }

                foreach (LiveWireLateralPath path in paths)
                {
                    this.screenOverlay.Paths.Add(path);
                }
            }

            this.detectionToolbox.UncheckToggleButtons(null);
            this.screenOverlay.IsBusy = false;
            this.statusText.Text = "Status: Idle";
        }


        private void UpdateScreenImageAndZoom(WriteableBitmap wbmp)
        {
            Screen.Width = wbmp.PixelWidth;
            Screen.Height = wbmp.PixelHeight;
            UpdateScreenImage(wbmp);

            if (!loadImageZoomOnce)
            {
                ScreenScrollViewer.SelectOptimumZoomLevel(Screen.ActualWidth, Screen.ActualHeight, wbmp.PixelWidth, wbmp.PixelHeight);
                loadImageZoomOnce = true;
            }

            UpdateStatusText("Status: Idle");
        }

        private void UpdateScreenImage(WriteableBitmap wbmp)
        {
            ScreenImage.ImageSource = wbmp;


        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl)
            {
                this.screenOverlay.RemoveTipHighlight();
            }
            
            base.OnKeyDown(e);
        }

        unsafe void EMManagerProgressCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            this.Dispatcher.Invoke(new StatusTextUpdateDelegate(this.UpdateStatusText), "Status: Rendering Image");

            WriteableBitmap wbmp = new WriteableBitmap(this.emManager.Width, this.emManager.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.featureBitmap = new WriteableBitmap(this.emManager.Width, this.emManager.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            this.probabilityMapBestClass = new double[this.emManager.Width * this.emManager.Height];
            this.probabilityMapBrightestClass = new double[this.emManager.Width * this.emManager.Height];
            this.probabilityMapSecondClass = new double[this.emManager.Width * this.emManager.Height];
            this.screenOverlay.PatchSize = this.emManager.Configuration.PatchSize;
            
            foreach (KeyValuePair<EMPatch, GaussianMixtureModel> Pair in this.emManager.Mixtures)
            {
                EMPatch currentPatch = Pair.Key;
                GaussianMixtureModel currentModel = Pair.Value;
                currentModel.CalculateBounds();
                UpdateImageOnPatchChange(currentPatch, currentModel, wbmp, this.featureBitmap);
            }

            if (wbmp.CanFreeze)
            {
                wbmp.Freeze();
            }

            if (featureBitmap.CanFreeze)
            {
                featureBitmap.Freeze();
            }

            this.screenOverlay.IsBusy = false;

            this.probabilityBitmap = wbmp;

            this.distanceProbabilityMap = DistanceMap.CreateDistanceMap(featureBitmap);

            /*
            int w = featureBitmap.PixelWidth;
            int h = featureBitmap.PixelHeight;

            System.Drawing.Bitmap b = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int col = (int)(distanceProbabilityMap[x, y] * 255);
                    b.SetPixel(x, y, System.Drawing.Color.FromArgb(col, col, col));
                }
            }

            b.Save("C:\\Users\\mpound\\Desktop\\distanceweights.bmp");
            */

            this.Dispatcher.Invoke(new ScreenImageUpdateDelegate(this.UpdateScreenImageAndZoom), wbmp);
            this.Dispatcher.Invoke(new EMCompletedDelegate(this.EMTaskCompleted), null);

            BeginTipDetection();
        }

        public void BeginTipDetection()
        {
            TipDetectionWorker tdw = new TipDetectionWorker();
            tdw.FeatureBitmap = this.featureBitmap;
            tdw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TipDetectionCompleted);
            this.detectionToolbox.cornerDetectionBorder.Visibility = System.Windows.Visibility.Hidden;
            this.detectionToolbox.cornerProcessingBorder.Visibility = System.Windows.Visibility.Visible;
            tdw.RunWorkerAsync();
        }

        void TipDetectionCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TipDetectionWorker tdw = sender as TipDetectionWorker;
            List<Int32Point> points = null;
            if (tdw != null)
            {
                points = tdw.Points;
            }
       
            if (points != null)
            {
                this.screenOverlay.TipAnchorPoints.Clear();

                //this.screenOverlay.ResetAll();

                foreach (Int32Point p in points)
                {
                    this.screenOverlay.TipAnchorPoints.Add((Point)p);

                   //this.screenOverlay.Terminals.Add((Point)p, TerminalType.Undefined, false);
                }
            }

            int count = this.screenOverlay.TipAnchorPoints.Count;
            this.detectionToolbox.tipDetectionLabel.Content = count == 1 ? "1 Tip Detected" : count.ToString() + " Tips Detected";
            this.detectionToolbox.cornerDetectionBorder.Visibility = System.Windows.Visibility.Visible;
            this.detectionToolbox.cornerProcessingBorder.Visibility = System.Windows.Visibility.Hidden;
            this.screenOverlay.InvalidateVisual();
        }

        public void ShowGMM(Point mousePosition)
        {
            // GMM updating
            if (this.emManager != null && this.emManager.IsWorkCompleted)
            {
                int[] histogram = this.emManager.PatchHistogramDataFromPoint(mousePosition);
                highlightedMixture = this.emManager.PatchGaussianMixtureModelFromPoint(mousePosition);
                highlightedMixture.CalculateBounds();
                this.ModelViewer.SetData(histogram, highlightedMixture, this.emManager.Configuration);
                if (this.HistogramViewBorder.IsHidden)
                    this.HistogramViewBorder.Show();
            }
        }

        public void AddControlPointToRoot(int highlightedRootIndex, Point dragPoint, Point newPoint)
        {
            if (this.screenOverlay.Paths[highlightedRootIndex] is LiveWirePrimaryPath)
            {
                LiveWirePrimaryPath currentPath = this.screenOverlay.Paths[highlightedRootIndex] as LiveWirePrimaryPath;

                if (currentPath == null)
                {
                    return;
                }

                int newIndex = currentPath.Path.IndexOf(dragPoint);
                if (currentPath.IntermediatePoints.Count > 0)
                {
                    bool indexFound = false;
                    for (int i = 0; i < currentPath.IntermediatePoints.Count; i++)
                    {
                        if (newIndex < currentPath.Indices[i])
                        {
                            currentPath.IntermediatePoints.Insert(i, newPoint);
                            indexFound = true;
                            break;
                        }
                    }
                    if (!indexFound)
                    {
                        currentPath.IntermediatePoints.Add(newPoint);
                    }

                }
                else
                {
                    currentPath.IntermediatePoints.Add(newPoint);
                }

                ReprocessAlteredRoot(highlightedRootIndex);
            }
            else if (this.screenOverlay.Paths[highlightedRootIndex] is LiveWireLateralPath)
            {
                LiveWireLateralPath currentPath = this.screenOverlay.Paths[highlightedRootIndex] as LiveWireLateralPath;

                if (currentPath == null)
                {
                    return;
                }

                int newIndex = currentPath.Path.IndexOf(dragPoint);
                if (currentPath.IntermediatePoints.Count > 0)
                {
                    bool indexFound = false;
                    for (int i = 0; i < currentPath.IntermediatePoints.Count; i++)
                    {
                        if (newIndex < currentPath.Indices[i])
                        {
                            currentPath.IntermediatePoints.Insert(i, newPoint);
                            indexFound = true;
                            break;
                        }
                    }
                    if (!indexFound)
                    {
                        currentPath.IntermediatePoints.Add(newPoint);
                    }

                }
                else
                {
                    currentPath.IntermediatePoints.Add(newPoint);
                }

                ReprocessLateralRoot(highlightedRootIndex);
            }
        }

        
        public void ReprocessAlteredRoot(params int[] rootIndexes)
        {
            this.screenOverlay.IsBusy = true;
            this.statusText.Text = "Status: Recalculating " + rootIndexes.Length.ToString() + (rootIndexes.Length == 1 ? " root" : " roots");
            int width = this.emManager.Width;
            int height = this.emManager.Height;

            int threadCount = Core.Threading.ThreadParams.LiveWireThreadCount;

            List<LiveWirePrimaryPath> alteredPaths = new List<LiveWirePrimaryPath>();

            foreach (int i in rootIndexes)
            {
                LiveWirePrimaryPath p = this.screenOverlay.Paths[i] as LiveWirePrimaryPath;
                if (p != null)
                {
                    alteredPaths.Add(p);
                }
            }

            LiveWirePrimaryManager manager = new LiveWirePrimaryManager()
            {
                Graph = this.currentGraph,
                Terminals = this.screenOverlay.Terminals,
                ThreadCount = Math.Min(rootIndexes.Length, threadCount),
                DistanceMap = this.distanceProbabilityMap,
                ReWorkPaths = alteredPaths
            };

            manager.ProgressChanged += new ProgressChangedEventHandler(LiveWireManagerProgressChanged);
            manager.ProgressCompleted += new RunWorkerCompletedEventHandler(LiveWireManagerReProgressCompleted);
            manager.ReRun();
        }

        public void AnalysePrimaryRoots()
        {
            if (this.screenOverlay.IsBusy
                || this.screenOverlay.Terminals.Sources.Count() == 0
                || this.screenOverlay.Terminals.Primaries.Count() == 0)
            {
                return;
            }

            this.statusText.Text = "Status: Generating probability map";
            this.screenOverlay.IsBusy = true;

            int width = this.emManager.Width;
            int height = this.emManager.Height;



            this.currentGraph = LiveWireGraph.FromProbabilityMap(this.probabilityMapBestClass, width, height);

            int combinations = this.screenOverlay.Terminals.UnlinkedSources.Count() * this.screenOverlay.Terminals.UnlinkedPrimaries.Count() + this.screenOverlay.Terminals.TerminalLinks.Count();

            int threadCount = Math.Min(Core.Threading.ThreadParams.LiveWireThreadCount, combinations);
            
            this.statusText.Text = "Status: Examining " + combinations.ToString() + " potential" + (combinations == 1 ? " root" : " roots");

            this.primaryLiveWireManager = new LiveWirePrimaryManager()
            {
                DistanceMap = this.distanceProbabilityMap,
                Graph = this.currentGraph,
                Terminals = this.screenOverlay.Terminals,
                ThreadCount = threadCount
            };

            primaryLiveWireManager.ProgressChanged += new ProgressChangedEventHandler(LiveWireManagerProgressChanged);
            primaryLiveWireManager.ProgressCompleted += new RunWorkerCompletedEventHandler(LiveWireManagerProgressCompleted);
            primaryLiveWireManager.Run();
        }

        public void ReprocessLateralRoot(params int[] rootIndexes)
        {
            this.screenOverlay.IsBusy = true;
            this.statusText.Text = "Status: Recalculating " + rootIndexes.Length.ToString() + (rootIndexes.Length == 1 ? " root" : " roots");
            int width = this.emManager.Width;
            int height = this.emManager.Height;

            int threadCount = Core.Threading.ThreadParams.LiveWireThreadCount;

            List<LiveWireLateralPath> alteredPaths = new List<LiveWireLateralPath>();

            foreach (int i in rootIndexes)
            {
                LiveWireLateralPath p = this.screenOverlay.Paths[i] as LiveWireLateralPath;
                if (p != null)
                {
                    alteredPaths.Add(p);
                }
            }

            this.lateralLiveWireManager = new LiveWireLateralManager()
            {
                Graph = currentGraph,
                Terminals = this.screenOverlay.Terminals,
                ThreadCount = Math.Max(rootIndexes.Length, threadCount),
                CurrentPaths = this.screenOverlay.Paths.Primaries.ToList(),
                ReWorkPaths = alteredPaths,
                DistanceMap = this.distanceProbabilityMap
            };
            lateralLiveWireManager.ProgressChanged += new ProgressChangedEventHandler(LiveWireManagerProgressChanged);
            lateralLiveWireManager.ProgressCompleted += new RunWorkerCompletedEventHandler(LateralLiveWireManagerReProgressCompleted);
            lateralLiveWireManager.ReRun();
        }

        public void AnalyseLateralRoots()
        {
            if (this.screenOverlay.IsBusy
                || this.screenOverlay.Paths.Primaries.Count() == 0
                || this.screenOverlay.Terminals.Laterals.Count() == 0)
            {
                return;
            }

            this.screenOverlay.IsBusy = true;

            int width = this.emManager.Width;
            int height = this.emManager.Height;

            int threadCount = Core.Threading.ThreadParams.LiveWireThreadCount;

            if (this.currentGraph == null)
                this.currentGraph = LiveWireGraph.FromProbabilityMap(this.probabilityMapBestClass, width, height);

            int lateralCount = this.screenOverlay.Terminals.Laterals.Count();
            this.statusText.Text = "Status: Examining " + lateralCount.ToString() + " lateral" + (lateralCount == 1 ? " root" : " roots");

            this.lateralLiveWireManager = new LiveWireLateralManager()
            {
                Graph = currentGraph,
                Terminals = this.screenOverlay.Terminals,
                CurrentPaths = this.screenOverlay.Paths.Primaries.ToList(),
                ThreadCount = Math.Min(lateralCount, threadCount),
                DistanceMap = this.distanceProbabilityMap
            };
            lateralLiveWireManager.ProgressChanged += new ProgressChangedEventHandler(LiveWireManagerProgressChanged);
            lateralLiveWireManager.ProgressCompleted += new RunWorkerCompletedEventHandler(LiveWireManagerProgressCompleted);
            lateralLiveWireManager.Run();
        }

        void LateralLiveWireManagerReProgressCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            // UI
            this.Dispatcher.BeginInvoke(new LiveWireReCompletedDelegate(this.LiveWireReWorkCompletedUI));
        }

        private void GaussianControllerParametersChanged()
        {
            this.ModelViewer.WeightedComponents = (bool)this.ModelController.weightedComponentsCheckbox.IsChecked;
            this.ModelViewer.ShowMixtureDistribution = (bool)this.ModelController.mixtureDistributionCheckbox.IsChecked;
            this.ModelViewer.ScaleXAxis = (bool)this.ModelController.autoscaleXAxis.IsChecked;
            this.ModelViewer.Normalised = (bool)this.ModelController.normalisedCheckbox.IsChecked;
            this.ModelViewer.InvalidateVisual();
        }

        private void ModelCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.HistogramViewBorder.Hide();
        }

        unsafe public void AlterCurrentGMMThreshold(bool relax)
        {
            // GMM updating
            if (highlightedMixture != null)
            {
                if (relax && highlightedMixture.ThresholdK >= 0)
                {
                    highlightedMixture.ThresholdK--;
                }
                else if (!relax && highlightedMixture.ThresholdK < highlightedMixture.K - 1)
                {
                    highlightedMixture.ThresholdK++;
                }

                highlightedMixture.CalculateBounds();
                this.ModelViewer.ReCalculateData();
                if (this.HistogramViewBorder.IsHidden)
                    this.HistogramViewBorder.Show();
            }

            // Image updating
            WriteableBitmap wbmp = new WriteableBitmap(this.probabilityBitmap);

            if (featureBitmap.IsFrozen)
            {
                featureBitmap = new WriteableBitmap(featureBitmap);
            }

            if (wbmp != null)
            {
                EMPatch currentPatch = null;
                GaussianMixtureModel currentModel = null;
                foreach (KeyValuePair<EMPatch, GaussianMixtureModel> Pair in this.emManager.Mixtures)
                {
                    if (highlightedMixture == Pair.Value)
                    {
                        currentPatch = Pair.Key;
                        currentModel = Pair.Value;
                        break;
                    }
                }
               

                UpdateImageOnPatchChange(currentPatch, currentModel, wbmp, featureBitmap);

                if (wbmp.CanFreeze)
                {
                    wbmp.Freeze();
                }
                this.probabilityBitmap = wbmp;
                this.Dispatcher.Invoke(new ScreenImageUpdateDelegate(this.UpdateScreenImage), wbmp);
            }

            if (featureBitmap.CanFreeze)
            {
                featureBitmap.Freeze();
            }

            this.distanceProbabilityMap = DistanceMap.CreateDistanceMap(featureBitmap);

            BeginTipDetection();
        }

        unsafe private void UpdateImageOnPatchChange(EMPatch patch, GaussianMixtureModel model, WriteableBitmap screenBitmap, WriteableBitmap featureBitmap)
        {
            int width = emManager.Width;
            int height = emManager.Height;
            EMConfiguration config = emManager.Configuration;
            
            screenBitmap.Lock();
            featureBitmap.Lock();
            uint* outputPointer = (uint*)screenBitmap.BackBuffer.ToPointer();
            byte* featurePointer = (byte*)featureBitmap.BackBuffer.ToPointer();

            int outputStride = screenBitmap.BackBufferStride / 4;
            int featureStride = featureBitmap.BackBufferStride;

            if (patch != null && model != null)
            {
                model.CalculateBounds();
                int left = patch.Left, right = patch.Right, top = patch.Top, bottom = patch.Bottom;

                // Probabilities for the intensities in this patch
                int cK = model.K;
                double[,] normalisedProbabilities = new double[256, cK];
                int[] probabilityMaximums = new int[256];
                for (int intensity = 0; intensity < 256; intensity++)
                {
                    double max = double.MinValue;
                    for (int k = 0; k < cK; k++)
                    {
                        // Normalised probability
                        normalisedProbabilities[intensity, k] = model.NormalisedProbability(intensity, k);
                        
                        // Maximum for this intensity
                        if (normalisedProbabilities[intensity, k] > max)
                        {
                            max = normalisedProbabilities[intensity, k];
                            probabilityMaximums[intensity] = k;
                        }
                    }
                }

                // Render patch data to image and probability map
                for (int y = top; y < bottom; y++)
                {
                    for (int x = left; x < right; x++)
                    {
                        uint bgr32 = 0;
                        int thresholdedK = model.K - model.ThresholdK - 1;

                        int index = y * width + x;
                        int bufferValue = intensityBuffer[index];
                        int k = probabilityMaximums[bufferValue];
                        double p = normalisedProbabilities[bufferValue, k];

                        // p is the probability of the most probable class at this pixel.
                        // k is the most probable class
                        // thresholdedK is the number of classes above the threshold value.

                        if (k > model.ThresholdK && thresholdedK >= config.ExpectedRootClassCount)
                        {
                            int weightIndex = Math.Max(0, k - model.K + config.Weights.Length);

                            // Most probable class
                            this.probabilityMapBestClass[index] = normalisedProbabilities[bufferValue, k] * config.Weights[weightIndex];

                            // Highest intensity class, regardless of probability
                            this.probabilityMapBrightestClass[index] = normalisedProbabilities[bufferValue, model.K - 1] * config.Weights[config.Weights.Length - 1];

                            // Second intensity class, regardless of probability - if one exists
                            this.probabilityMapSecondClass[index] = normalisedProbabilities[bufferValue, model.K - 2] * config.Weights[Math.Max(0, config.Weights.Length - 2)];

                            int pixelIntensity = (int)(this.probabilityMapBestClass[index] * 255);

                            if (k == model.K - 1)
                            {
                                int blue = 127 + (pixelIntensity / 2);
                                bgr32 = (uint)blue | 80 << 8 | 80 << 16;
                            }
                            else
                            {
                                bgr32 = (uint)(0 | pixelIntensity << 8);
                            }
                        }
                        else
                        {
                            this.probabilityMapBestClass[index] = 0.0;
                            this.probabilityMapBrightestClass[index] = 0.0;
                            this.probabilityMapSecondClass[index] = 0.0;
                            bgr32 = 0;
                        }

                        *(featurePointer + (y * featureStride) + x) = (byte)(this.probabilityMapBrightestClass[index] * 255);
                        *(outputPointer + (y * outputStride) + x) = bgr32;
                    }
                }

                screenBitmap.AddDirtyRect(new Int32Rect(0, 0, screenBitmap.PixelWidth, screenBitmap.PixelHeight));
                screenBitmap.Unlock();

                featureBitmap.AddDirtyRect(new Int32Rect(0, 0, featureBitmap.PixelWidth, featureBitmap.PixelHeight));
                featureBitmap.Unlock();

                // Saves the blue channel out to a probability map
                //ImageEncoder.SaveImage(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\featuremap.png", featureBitmap, ImageEncoder.EncodingType.PNG);
            } 
        }

        unsafe private void ModelThresholdChanged(int threshold)
        {
            // GMM updating
            if (highlightedMixture != null)
            {
                highlightedMixture.ThresholdAtIntensity(threshold);
                highlightedMixture.CalculateBounds();
                this.ModelViewer.ReCalculateData();
                if (this.HistogramViewBorder.IsHidden)
                    this.HistogramViewBorder.Show();
            }

            // Image updating
            WriteableBitmap wbmp = new WriteableBitmap(this.probabilityBitmap);

            if (featureBitmap.IsFrozen)
            {
                featureBitmap = new WriteableBitmap(featureBitmap);
            }

            if (wbmp != null)
            {
                EMPatch currentPatch = null;
                GaussianMixtureModel currentModel = null;
                foreach (KeyValuePair<EMPatch, GaussianMixtureModel> Pair in this.emManager.Mixtures)
                {
                    if (highlightedMixture == Pair.Value)
                    {
                        currentPatch = Pair.Key;
                        currentModel = Pair.Value;
                        break;
                    }
                }

                UpdateImageOnPatchChange(currentPatch, currentModel, wbmp, featureBitmap);

                if (wbmp.CanFreeze)
                {
                    wbmp.Freeze();
                }
                this.probabilityBitmap = wbmp;
                this.Dispatcher.Invoke(new ScreenImageUpdateDelegate(this.UpdateScreenImage), wbmp);
            }
        }

        private void ScreenLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            string filePath = "";
            if (PromptLoadImage(out filePath))
            {
                if (VerifyImage(filePath))
                {
                    LoadImages(filePath);
                }
                else
                {
                    this.StartScreenLabel.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void PathInfoCloseButton_Click(object sender, RoutedEventArgs e)
        { 
            this.PathInfoViewBorder.Hide();
        }

        private static Random random = new Random();
        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public void MeasureRootSystem()
        {
            if (this.screenOverlay.Roots == null || this.screenOverlay.Roots.RootTree.Count < 1)
            {
                return;
            }

            // Tagging or cancel
            TagBox t = new TagBox(this.FileName);
            bool? addtag = t.ShowDialog();
            if (!addtag.HasValue || t.Cancelled)
            {
                return;
            }
            
            string tag = t.Text;
            if (!addtag.Value || tag == "")
            {
                // Generate random string
                tag = RandomString(10);
            }

            if (connectionExists && this.connectionInfo != null)
            {
                if (this.connectionInfo.Source == ConnectionSource.MySQLDatabase)
                {
                    BitmapSource source = (bool)this.measurementToolbox.outputImageCheckbox.IsChecked ? this.sourceBitmap : null;
                    bool success = RootMeasurement.WriteToDatabase(databaseManager as MySQLDatabaseManager, tag, (bool)this.measurementToolbox.completeArchitectureOutputCheckbox.IsChecked, this.screenOverlay.Roots.ToList(), source);
                    if (success)
                    {
                        UpdateStatusText("Status: Measurements successfully output to database");
                    }
                    else
                    {
                        MessageBox.Show("Database could not be written to", "Database Error");
                        UpdateStatusText("Status: Measurements could not be written to the database");
                    }
                }

                if (this.connectionInfo.Source == ConnectionSource.RSMLDirectory)
                {
                    // Create instance of writer class
                    RootNav.Data.IO.RSML.RSMLRootWriter writer = new Data.IO.RSML.RSMLRootWriter(connectionInfo);
                    
                    // Create Scene and Metadata
                    SceneMetadata metadata = RootFormatConverter.RootNavDataToRSMLMetadata(this.imageInfo, this.imageHeaderInfo, tag, this.screenOverlay.Roots);
                    SceneInfo scene = RootFormatConverter.RootCollectionToRSMLScene(this.screenOverlay.Roots);

                    if (!(bool)this.measurementToolbox.completeArchitectureOutputCheckbox.IsChecked)
                    {
                        RootFormatConverter.SetIncompletePropertyOnScene(metadata, scene);
                    }

                    bool success = writer.Write(metadata, scene);
                    if (success)
                    {
                        UpdateStatusText("Status: Measurements successfully output to RSML file");
                    }
                    else
                    {
                        UpdateStatusText("Status: Measurements could not be written to RSML file");
                    }
                }
            }

            // Output measurent table if requested
            if ((bool)this.measurementToolbox.measurementOutputCheckbox.IsChecked || !connectionExists)
            {
                MeasurementWindow mw = new MeasurementWindow();

                foreach (Dictionary<string, string> data in RootMeasurement.GetDataAsStrings(this.screenOverlay.Roots.RootTree.ToList()))
                {
                    if (addtag.Value)
                    {
                        data.Add("Tag", tag);
                    }
                    mw.Add(data);
                }
                
                mw.Show();
            }

            // TODO: Could build curvature and map profiles into root functions in RSML

            #region Curvature Profile
            if ((bool)this.measurementToolbox.curvatureProfileCheckbox.IsChecked)
            {
                TableWindow tw = new TableWindow();

                var data = RootMeasurement.GetCurvatureProfiles(this.screenOverlay.Roots.RootTree.ToList(), 4);

                double minDistance = double.MinValue;
                int rowCount = 0;
                foreach (var v in data)
                {
                    if (v.Value != null && v.Value.Length > rowCount)
                    {
                        if (v.Value[0].Item1 > minDistance)
                        {
                            minDistance = v.Value[0].Item1;
                        }

                        rowCount = v.Value.Length;
                    }
                }

                string[,] outputArray = new string[data.Count, rowCount + 1];

                int i = 1;
                foreach (var keyValuePair in data)
                {
                    if (keyValuePair.Value != null)
                    {
                        // Header
                        outputArray[i, 0] = keyValuePair.Key.RelativeID;

                        // Data
                        var distanceAnglePair = keyValuePair.Value;
                        if (distanceAnglePair.Length == rowCount)
                        {
                            for (int j = 0; j < distanceAnglePair.Length; j++)
                            {
                                outputArray[i, j + 1] = Math.Round(distanceAnglePair[j].Item2,1).ToString();
                                outputArray[0, j + 1] = Math.Round(distanceAnglePair[j].Item1).ToString();
                            }

                        }
                        else
                        {
                            for (int j = 0; j < distanceAnglePair.Length; j++)
                            {
                                outputArray[i, j + 1] = Math.Round(distanceAnglePair[j].Item2, 1).ToString();
                            }
                        }

                        i++;
                    }
                    
                }

               
                // Turn array into data table
                DataTable dt = new DataTable();
                int nbColumns = outputArray.GetLength(0);
                int nbRows = outputArray.GetLength(1);
                for (i = 0; i < nbColumns; i++)
                {
                    dt.Columns.Add(i.ToString(), typeof(string));
                }

                for (int row = 0; row < nbRows; row++)
                {
                    DataRow dr = dt.NewRow();
                    for (int col = 0; col < nbColumns; col++)
                    {
                        dr[col] = outputArray[col,row];
                    }
                    dt.Rows.Add(dr);
                }

                tw.measurementsView.ItemsSource = dt.DefaultView;

                tw.Show();
            }
            #endregion

            #region Map Profile
            if ((bool)this.measurementToolbox.mapProfileCheckbox.IsChecked)
            {
                TableWindow tw = new TableWindow();

                Dictionary<RootBase, Tuple<double, double>[]> leftData, rightData;

                RootMeasurement.GetMapProfiles(this.screenOverlay.Roots.RootTree.ToList(), out leftData, out rightData,
                                               4,
                                               this.measurementToolbox.travelSlider.Value,
                                               this.probabilityMapSecondClass,
                                               this.emManager.Width,
                                               this.emManager.Height);

                double minDistance = double.MinValue;
                int rowCount = 0;

                // Left
                foreach (var v in leftData)
                {
                    if (v.Value != null && v.Value.Length > rowCount)
                    {
                        if (v.Value[0].Item1 > minDistance)
                        {
                            minDistance = v.Value[0].Item1;
                        }

                        rowCount = v.Value.Length;
                    }
                }

                // Right
                foreach (var v in rightData)
                {
                    if (v.Value != null && v.Value.Length > rowCount)
                    {
                        if (v.Value[0].Item1 > minDistance)
                        {
                            minDistance = v.Value[0].Item1;
                        }

                        rowCount = v.Value.Length;
                    }
                }


                string[,] outputArray = new string[leftData.Count + rightData.Count - 1, rowCount + 1];

                int i = 1;
                foreach (var keyValuePair in leftData)
                {
                    if (keyValuePair.Value != null)
                    {
                        // Header
                        outputArray[i, 0] = keyValuePair.Key.RelativeID + " L";

                        // Left Data
                        var distanceMapPair = keyValuePair.Value;
                        if (distanceMapPair.Length == rowCount)
                        {
                            for (int j = 0; j < distanceMapPair.Length; j++)
                            {
                                outputArray[i, j + 1] = Math.Round(distanceMapPair[j].Item2, 1).ToString();
                                outputArray[0, j + 1] = Math.Round(distanceMapPair[j].Item1).ToString();
                            }

                        }
                        else
                        {
                            for (int j = 0; j < distanceMapPair.Length; j++)
                            {
                                outputArray[i, j + 1] = Math.Round(distanceMapPair[j].Item2, 1).ToString();
                            }
                        }

                        i+=2;
                    }

                }

                i = 2;
                foreach (var keyValuePair in rightData)
                {
                    if (keyValuePair.Value != null)
                    {
                        // Header
                        outputArray[i, 0] = keyValuePair.Key.RelativeID + " R";

                        // Left Data
                        var distanceMapPair = keyValuePair.Value;

                        for (int j = 0; j < distanceMapPair.Length; j++)
                        {
                            outputArray[i, j + 1] = Math.Round(distanceMapPair[j].Item2, 1).ToString();
                        }

                        i += 2;
                    }
                }

                // Turn array into data table
                DataTable dt = new DataTable();
                int nbColumns = outputArray.GetLength(0);
                int nbRows = outputArray.GetLength(1);
                for (i = 0; i < nbColumns; i++)
                {
                    dt.Columns.Add(i.ToString(), typeof(string));
                }

                for (int row = 0; row < nbRows; row++)
                {
                    DataRow dr = dt.NewRow();
                    for (int col = 0; col < nbColumns; col++)
                    {
                        dr[col] = outputArray[col, row];
                    }
                    dt.Rows.Add(dr);
                }

                tw.measurementsView.ItemsSource = dt.DefaultView;

                tw.Show();
            }
            #endregion
        }

        private RootNav.Data.IO.ConnectionParams connectionInfo = null;

        public bool OpenOutputSourceConnection(bool showWindow)
        {
            if (connectionInfo == null && showWindow)
            {
                DataSourceWindow dsw = new DataSourceWindow();
                bool? result = dsw.ShowDialog();
                if ((bool)result)
                {
                    connectionInfo = dsw.ConnectionInfo;
                }
            }

            if (connectionInfo != null)
            {
                if (connectionInfo.Source == ConnectionSource.MySQLDatabase)
                {
                    databaseManager = new RootNav.Data.IO.Databases.MySQLDatabaseManager();
                    bool success = databaseManager.Open(this.connectionInfo.ToMySQLConnectionString());

                    if (success)
                    {
                        this.connectionExists = true;
                        this.measurementToolbox.SetConnected("MySql Database", connectionInfo.Server);
                        return true;
                    }
                    else
                    {
                        this.connectionExists = false;
                        this.measurementToolbox.SetUnconnected();
                        return false;
                    }
                }
                else if (connectionInfo.Source == ConnectionSource.RSMLDirectory)
                {
                    // If directory must be created, create it
                    if (!Directory.Exists(connectionInfo.Directory))
                    {
                        try
                        {
                            if (Directory.CreateDirectory(connectionInfo.Directory).Exists)
                            {
                                return this.connectionExists = true;
                            }
                        }
                        catch
                        {
                            this.connectionExists = false;
                            this.measurementToolbox.SetUnconnected();
                            return false;
                        }

                        this.connectionExists = false;
                        this.measurementToolbox.SetUnconnected();
                        return false;
                    }
                    else
                    {
                        this.measurementToolbox.SetConnected("RSML Directory", connectionInfo.Directory);
                        this.connectionExists = true;
                        return true;
                    }
                }
            }

            return false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.screenOverlay.Stage == OverlayStage.Detection)
            {
                if (FocusManager.GetFocusedElement(this) != null && FocusManager.GetFocusedElement(this).GetType() != typeof(TextBox))
                {
                    switch (e.Key)
                    {
                        case Key.S:
                        case Key.D1:
                            this.detectionToolbox.AddRootSourceToggleButton.IsChecked = !this.detectionToolbox.AddRootSourceToggleButton.IsChecked;
                            break;
                        case Key.P:
                        case Key.D2:
                            this.detectionToolbox.AddPrimaryToggleButton.IsChecked = !this.detectionToolbox.AddPrimaryToggleButton.IsChecked;
                            break;
                        case Key.L:
                        case Key.D3:
                            this.detectionToolbox.AddLateralToggleButton.IsChecked = !this.detectionToolbox.AddLateralToggleButton.IsChecked;
                            break;
                        case Key.R:
                        case Key.D4:
                            this.detectionToolbox.RemoveRootTerminalToggleButton.IsChecked = !this.detectionToolbox.RemoveRootTerminalToggleButton.IsChecked;
                            break;
                        case Key.LeftShift:
                            StartShiftAdd();
                            break;
                        case Key.I:
                            if (this.showSourceImage)
                            {
                               this.ShowBackgroundProbabilityImage.IsChecked = true;
                            }
                            else
                            {
                                this.ShowBackgroundSourceImage.IsChecked = true;
                            }
                            break;
                        case Key.A:
                            this.AnalysePrimaryRoots();
                            break;
                        case Key.Z:
                            this.AnalyseLateralRoots();
                            break;
                    }
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.screenOverlay.Stage == OverlayStage.Detection)
            {
                if (FocusManager.GetFocusedElement(this) != null && FocusManager.GetFocusedElement(this).GetType() != typeof(TextBox))
                {
                    switch (e.Key)
                    {
                        case Key.LeftShift:
                            EndShiftAdd();
                            break;
                    }
                }
            }
        }

        System.Windows.Controls.Primitives.ToggleButton shiftAddPreviousButton = null;

        private void StartShiftAdd()
        {
            if (screenOverlay.LinkAdd == true)
            {
                return;
            }

            // Record previously toggled button
            if ((bool)this.detectionToolbox.AddRootSourceToggleButton.IsChecked)
            {
                shiftAddPreviousButton = this.detectionToolbox.AddRootSourceToggleButton;
            }
            else if ((bool)this.detectionToolbox.AddPrimaryToggleButton.IsChecked)
            {
                shiftAddPreviousButton = this.detectionToolbox.AddPrimaryToggleButton;
            }
            else if ((bool)this.detectionToolbox.AddLateralToggleButton.IsChecked)
            {
                shiftAddPreviousButton = this.detectionToolbox.AddLateralToggleButton;
            }
            else
            {
                shiftAddPreviousButton = null;
            }

            this.detectionToolbox.AddPrimaryToggleButton.IsChecked = true;
            this.screenOverlay.LinkAdd = true;
        }

        private void EndShiftAdd()
        {
            if (shiftAddPreviousButton != null)
            {
                shiftAddPreviousButton.IsChecked = true;
            }
            this.screenOverlay.LinkAdd = false;
        }

        private void MeasurementBackButton_Click(object sender, RoutedEventArgs e)
        {
            // Return to the detection stage
            double conversion;
            bool conversionExists = double.TryParse(this.preMeasurementToolbox.imageResolutionTextbox.Text, out conversion);

            if (!conversionExists)
            {
                conversion = 0;
            }

            this.screenOverlay.BackFromMeasurementStage();

            BindingOperations.ClearBinding(this.rootTreeView, TreeView.ItemsSourceProperty);
            this.rootTreeView.MouseMove -= new MouseEventHandler(rootTreeView_MouseMove);
            this.rootTreeView.MouseLeave -= new MouseEventHandler(rootTreeView_MouseLeave);

            this.rootTreeView.SelectedItemChanged -= new RoutedPropertyChangedEventHandler<object>(rootTreeView_SelectedItemChanged);

            this.detectionSlidePanel.BeginShow();
            this.measurementSlidePanel.BeginHide();
        }

        #region View background selection
        private void ShowBackgroundProbabilityImage_Checked(object sender, RoutedEventArgs e)
        {
            if (this.showSourceImage == false)
            {
                return;
            }

            if (this.probabilityBitmap != null)
            {
                UpdateScreenImage(this.probabilityBitmap);
                this.showSourceImage = false;
            }
            this.ShowBackgroundSourceImage.IsChecked = false;
        }

        private void ShowBackgroundSourceImage_Checked(object sender, RoutedEventArgs e)
        {
            if (this.showSourceImage == true)
            {
                return;
            }

            if (this.sourceBitmap != null)
            {
                UpdateScreenImage(this.sourceBitmap);
                this.showSourceImage = true;
            }
            this.ShowBackgroundProbabilityImage.IsChecked = false;
        }


        private void ShowBackgroundSourceImage_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.showSourceImage == true)
            {
                ShowBackgroundSourceImage.IsChecked = true;
            }
        }

        private void ShowBackgroundProbabilityImage_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.showSourceImage == false)
            {
                ShowBackgroundProbabilityImage.IsChecked = true;
            }
        }
        #endregion

        private void NewImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenCVY3 getYoloRes = new OpenCVY3();
            if (!this.screenOverlay.IsBusy)
            {
                string filePath;
                if (PromptLoadImage(out filePath))
                {
                    if (VerifyImage(filePath))
                    {
                        ResetAll();
                        LoadImages(filePath);

                        //Here class in Yolov3OpenCv is called. 
                        //predictions are collected and added according to their class. 
                        #region Yolov3 OpenCV upgrade starts here 
                        getYoloRes.yworker(filePath);

                        for (int i = 0; i < getYoloRes.myres.classIds.Count(); i++)
                        {
                            if (getYoloRes.myres.classIds[i] == 0)
                            {
                                this.detectionToolbox.Mode = DetectionToolbox.RootTerminalControlMode.AddPrimary;
                            }

                            if (getYoloRes.myres.classIds[i] == 1)
                            {
                                this.detectionToolbox.Mode = DetectionToolbox.RootTerminalControlMode.AddLateral;
                            }

                            if (getYoloRes.myres.classIds[i] == 2)
                            {
                                this.detectionToolbox.Mode = DetectionToolbox.RootTerminalControlMode.AddSource;  
                            }

                            this.screenOverlay.fakemouseclickdown(getYoloRes.myres.cpoints[i]);
                            this.screenOverlay.fakemouseclickup(getYoloRes.myres.cpoints[i]);
                            this.detectionToolbox.Mode = DetectionToolbox.RootTerminalControlMode.None;
                        };
                        #endregion
                        //code change ends here.
                    }
                    else if (this.sourceBitmap == null)
                    {
                        this.StartScreenLabel.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }

        public void ResetAll()
        {
            this.screenOverlay.ResetAll();

            this.screenOverlay.BackFromMeasurementStage();

            BindingOperations.ClearBinding(this.rootTreeView, TreeView.ItemsSourceProperty);
            this.rootTreeView.MouseMove -= new MouseEventHandler(rootTreeView_MouseMove);
            this.rootTreeView.MouseLeave -= new MouseEventHandler(rootTreeView_MouseLeave);

            this.rootTreeView.SelectedItemChanged -= new RoutedPropertyChangedEventHandler<object>(rootTreeView_SelectedItemChanged);
            
            UpdateScreenImage(null);

            // Reassign checkboxes
            if (this.showSourceImage == true)
            {
                // Uncheck source image box, remove handlers first
                this.ShowBackgroundSourceImage.Unchecked -= new System.Windows.RoutedEventHandler(this.ShowBackgroundSourceImage_Unchecked);
                this.ShowBackgroundSourceImage.IsChecked = false;
                this.ShowBackgroundSourceImage.Unchecked += new System.Windows.RoutedEventHandler(this.ShowBackgroundSourceImage_Unchecked);

                // Check map image box, remove handler first
                this.ShowBackgroundProbabilityImage.Checked -= new System.Windows.RoutedEventHandler(this.ShowBackgroundProbabilityImage_Checked);
                this.ShowBackgroundProbabilityImage.IsChecked = true;
                this.ShowBackgroundProbabilityImage.Checked += new System.Windows.RoutedEventHandler(this.ShowBackgroundProbabilityImage_Checked);

                this.showSourceImage = false;
            }
            
            if (!this.detectionSlidePanel.IsVisible)
            {
                this.detectionSlidePanel.BeginShow();
                this.measurementSlidePanel.BeginHide();
            }
        }

        public bool PromptLoadImage(out string path)
        {
            // Load additional images into the repository
            Microsoft.Win32.OpenFileDialog opfd = new Microsoft.Win32.OpenFileDialog();

            // Open a file dialog on the users desktop
            opfd.Filter = "Images (*.BMP;*.JPG;*.GIF;*.PNG;*.TIF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIF|" +
                          "All files (*.*)|*.*";

            // Other settings
            opfd.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            opfd.Multiselect = false;

            if ((bool)opfd.ShowDialog())
            {
                path = opfd.FileName;
                return true;
            }
            else
            {
                path = "";
                return false;
            }
        }

        private void ChanceSourceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataSourceWindow dsw = new DataSourceWindow();
            bool? result = dsw.ShowDialog();
            if ((bool)result)
            {
                connectionInfo = dsw.ConnectionInfo;
                OpenOutputSourceConnection(false);
            }
        }
    }

}

