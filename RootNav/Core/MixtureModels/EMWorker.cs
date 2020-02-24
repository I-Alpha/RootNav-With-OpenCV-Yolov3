using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace RootNav.Core.MixtureModels
{
    public class EMWorker : BackgroundWorker
    {
        public List<Tuple<EMPatch, GaussianMixtureModel>> Mixtures { get; set; }
        public EMConfiguration Configuration { get; set; }
        public List<EMPatch> Patches { get; set; }
        public byte[] IntensityBuffer { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        private double[] data = null;

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            List<Tuple<EMPatch, GaussianMixtureModel>> currentMixtures = new List<Tuple<EMPatch, GaussianMixtureModel>>();

            int patchProgressCount = 0;
            int totalPatches = Patches.Count;

            foreach (EMPatch patch in Patches)
            {
                // Check bounds
                if (patch.Left < 0 || patch.Right > Width || patch.Top < 0 || patch.Bottom > Height)
                    throw new EMPatch.EMPatchException("Patch falls outside the bounds of the source image");

                // Copy data
                int patchWidth = patch.Right - patch.Left;
                int patchHeight = patch.Bottom - patch.Top;
                int left = patch.Left, right = patch.Right, top = patch.Top, bottom = patch.Bottom;
                if (this.data == null || this.data.Length != patchWidth * patchHeight)
                    this.data = new double[patchWidth * patchHeight];

                int i = 0;
                double max = Double.MinValue, min = Double.MaxValue;
                for (int pX = left; pX < right; pX++)
                {
                    for (int pY = top; pY < bottom; pY++)
                    {
                        data[i] = IntensityBuffer[pY * Width + pX];
                        if (data[i] < min)
                            min = data[i];
                        if (data[i] > max)
                            max = data[i];
                        i += 1;
                    }
                }

                GaussianMixtureModel GMM = null;
                    
                // Run EM starting at the initial class count and increasing the class count if necessary
                for (int classCount = Configuration.InitialClassCount; classCount <= Configuration.MaximumClassCount; classCount++)
                {
                    // KMeans initialisation
                    GMM = GaussianMixtureModel.KMeansInitialisation(data, classCount, (int)min, (int)max);

                    // Expectation Maximisation
                    ExpectationMaximisation.Compute(data, GMM);

                    // Threshold
                    GMM.ThresholdAtPercentage(Configuration.BackgroundPercentage, Configuration.BackgroundExcessSigma);

                    // Check remaining classes for none (empty patch) or the correct number of classes
                    if (GMM.K - (GMM.ThresholdK + 1) >= Configuration.ExpectedRootClassCount
                        || GMM.K - (GMM.ThresholdK + 1) == 0)
                    {
                        break;
                    }
                }

                // Add Patch, GMM pair into output array
                if (GMM == null)
                {
                    throw new InvalidOperationException("GMM cannot be null");
                }

                currentMixtures.Add(new Tuple<EMPatch, GaussianMixtureModel>(patch, GMM));

                // Signal progress changed
                base.OnProgressChanged(new ProgressChangedEventArgs((int)(++patchProgressCount * 100.0 / totalPatches), null));
            }

            // Assign this.Mixtures
            this.Mixtures = currentMixtures;
        }
    }
}
