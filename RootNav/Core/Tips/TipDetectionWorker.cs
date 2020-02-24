using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace RootNav.Core.Tips
{
    class TipDetectionWorker : BackgroundWorker
    {
        public TipDetectionWorker()
            : base()
        {
            this.WorkerReportsProgress = false;
            this.WorkerSupportsCancellation = false;
        }

        WriteableBitmap featureBitmap = null;

        public WriteableBitmap FeatureBitmap
        {
            get { return featureBitmap; }
            set { featureBitmap = value; }
        }

        List<Tuple<Int32Point, double>> weightedPoints = null;

        public List<Tuple<Int32Point, double>> WeightedPoints
        {
            get { return weightedPoints; }
            set { weightedPoints = value; }
        }

        public List<Int32Point> Points
        {
            get
            {
                if (weightedPoints == null)
                {
                    return null;
                }

                return (from pair in weightedPoints select pair.Item1).ToList();
            }
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            if (featureBitmap == null)
            {
                return;
            }

            HarrisCornerDetector hcd = new HarrisCornerDetector
            {
                GaussianSigma = 2,
                LowerThreshold = 1000,
                UpperThreshold = 100000,
                K = 0.07
            };

            int sourceWidth = featureBitmap.PixelWidth;
            int sourceHeight = featureBitmap.PixelHeight;
            int optimumDimension = 300;

            // Calculate optimum scaling amount
            double optimumscale = optimumDimension / (double)(sourceWidth >= sourceHeight ? sourceWidth : sourceHeight);

            // Find nearest pixel size after scaling
            int scaledWidth = (int)(sourceWidth * optimumscale + 0.5);
            int scaledHeight = (int)(sourceHeight * optimumscale + 0.5);

            double xScale = sourceWidth / (double)scaledWidth;
            double yScale = sourceHeight / (double)scaledHeight;

            WriteableBitmap smaller = RootNav.IO.ImageConverter.Resize8bpp(featureBitmap, scaledWidth, scaledHeight);

            List<Tuple<Int32Point, double>> points = hcd.FindCorners(smaller);

            // Scale points back to original locations
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = new Tuple<Int32Point, double>(new Int32Point((int)(points[i].Item1.X * xScale + 0.5), (int)(points[i].Item1.Y * yScale + 0.5)), points[i].Item2);
            }

            // Detect tips on the original image, not the smaller version
            TipFeatures tf = new TipFeatures();            
            weightedPoints = tf.MatchFeatures(points, featureBitmap);
        }
    }
}
