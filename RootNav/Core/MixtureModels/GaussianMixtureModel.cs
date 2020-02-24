using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.Core.MixtureModels
{
    public class GaussianMixtureModel
    {
        public int K { get; set; }
        public int ThresholdK { get; set; }
        private double ThresholdedMixingWeight { get; set; }
        public double[] MixingWeight { get; set; }
        public double[] Mean { get; set; }
        public double[] Sigma { get; set; }

        public int lowerBound = 0;
        public int upperBound = 0;

        public GaussianMixtureModel(int k, double[] mixingweight, double[] mean, double[] sigma)
        {
            if (mixingweight.Length != k || mean.Length != k || sigma.Length != k)
                throw new Exception("Invalid parameters supplied to GMM constructor");

            this.K = k;
            this.MixingWeight = mixingweight;
            this.Mean = mean;
            this.Sigma = sigma;
        }

        public double SingleGaussianProbabilityEM(double x, int k)
        {
            if (k >= K)
            {
                return 0.0;
            }

            double f1 = 1 / (System.Math.Sqrt(2 * System.Math.PI * this.Sigma[k]));
            double f2 = Math.Pow(System.Math.E, -0.5 * System.Math.Pow(System.Math.Abs(x - this.Mean[k]) / this.Sigma[k],2.0));
            return f1 * f2;
        }

        public double SingleGaussianProbability(double x, int k)
        {
            if (k >= K)
            {
                return 0.0;
            }
            
            double f1 = 1 / (System.Math.Sqrt(2 * System.Math.PI * Math.Pow(this.Sigma[k],2.0)));
            double f2 = Math.Pow(System.Math.E, -0.5 * System.Math.Pow(System.Math.Abs(x - this.Mean[k]) / this.Sigma[k], 2.0));
            return f1 * f2;
        }

        public void CalculateBounds()
        {
            // Lower bound
            double maximum = double.MinValue;
            for (int i = 0; i < 255; i++)
            {
                double total = 0.0;
                for (int k2 = 0; k2 < this.K; k2++)
                {
                    total += this.SingleGaussianProbability(i, k2) * this.MixingWeight[k2];
                }
                double currentValue = (this.SingleGaussianProbability(i, 0) * this.MixingWeight[0] / total);
                if (currentValue > maximum)
                {
                    this.lowerBound = i;
                    maximum = currentValue;
                }
            }

            // Upper bound
            maximum = double.MinValue;
            for (int i = 255; i >= 0; i--)
            {
                double total = 0.0;
                for (int k2 = 0; k2 < this.K; k2++)
                {
                    total += this.SingleGaussianProbability(i, k2) * this.MixingWeight[k2];
                }
                double currentValue = (this.SingleGaussianProbability(i, this.K - 1) * this.MixingWeight[this.K - 1] / total);
                if (currentValue > maximum)
                {
                    this.upperBound = i;
                    maximum = currentValue;
                }
            }
        }



        public double NormalisedProbability(double x, int k)
        {
            // Special case - Above the l of right most component
            x = Math.Min(x, this.upperBound);
            
            // Special case - Below the intensity of the lower bound
            x = Math.Max(x, this.lowerBound);

            double total = 0.0;
            for (int k2 = 0; k2 < this.K; k2++)
            {
                total += this.SingleGaussianProbability(x, k2) * this.MixingWeight[k2];
            }

            return (this.SingleGaussianProbability(x, k) * this.MixingWeight[k] / total);
        }

        public double MixingProbability(double x)
        {
            if (this.ThresholdK >= this.K - 1)
                return 0.0;

            // For each component in the mixture
            double probabilty = 0.0;
            for (int k = this.ThresholdK + 1; k < this.K; k++)
                probabilty += this.MixingWeight[k] * this.SingleGaussianProbability(x, k);

            return probabilty;
        }

        public double MixtureMaximum(out int maximumk)
        {
            int maxk = 0;
            double max = double.MinValue;

            if (this.ThresholdK >= this.K - 1)
            {
                maximumk = 0;
                return 0.0;
            }

            for (int k = this.ThresholdK + 1; k < this.K; k++)
            {
                double probabilty = this.MixingWeight[k] * this.SingleGaussianProbability(this.Mean[k], k);
                if (probabilty > max)
                {
                    max = probabilty;
                    maxk = k;
                }
            }

            maximumk = maxk;
            return max;
        }

        public void OptimumComponentProbability(double x, out int k, out double p, out double m)
        {
            if (this.K == this.ThresholdK + 1) { k = 0; p = 0.0; m = 0.0; return; }

            int maxK = 0;
            double maxProb = double.MinValue;
            for (int i = this.ThresholdK + 1; i < this.K; i++)
            {
                double currentProb = this.MixingWeight[i] * this.SingleGaussianProbability(x, i) / ThresholdedMixingWeight;
                if (currentProb > maxProb)
                {
                    maxProb = currentProb;
                    maxK = i;
                }
            }

            k = maxK;
            p = this.SingleGaussianProbability(x, maxK) / ThresholdedMixingWeight;
            m = SingleGaussianProbability(this.Mean[maxK], maxK) / ThresholdedMixingWeight;
        }

        public void ThresholdAtIntensity(int intensity)
        {
            int k = 0;
            for (; k < this.K; k++)
            {
                if (this.Mean[k] > intensity)
                {
                    break;
                }
            }

            // All gaussians up to and including k are background
            this.ThresholdK = k - 1;
            this.ThresholdedMixingWeight = 0.0;
            for (int i = k + 1; i < K; i++)
                this.ThresholdedMixingWeight += this.MixingWeight[i];
        }

        public void ThresholdAtPercentage(double t, double u)
        {
            // Find the first k where the sum of all distributions up to and including k is greater than t
            double cumulativeWeight = 0.0;
            int k = 0;
            for (; k < this.K; k++)
            {
                cumulativeWeight += this.MixingWeight[k];
                if (cumulativeWeight > t)
                {
                    break;
                }
            }

            // Travel onwards from k by u standard deviations. Any gaussians in this area have undoubtedly converged on the same point.
            double upperThreshold = this.Mean[k] + u * this.Sigma[k];
            for (int k2 = k + 1; k2 < this.K; k2++)
            {
                if (this.Mean[k2] < upperThreshold)
                {
                    k = k2;
                }

            }

            // All gaussians up to and including k are background
            this.ThresholdK = k;
            this.ThresholdedMixingWeight = 0.0;
            for (int i = k + 1; i < K; i++)
                this.ThresholdedMixingWeight += this.MixingWeight[i];
        }

        /*
        public double MaxValue(out int m)
        {
            double maxVal = double.MinValue;
            int maxK = 0;
            for (int k = 0; k < this.K; k++)
            {
                double current = this.SingleGaussianProbability(this.Mean[k], k);
                if (current > maxVal)
                {
                    maxVal = current;
                    maxK = k;
                }

            }

            if (this.K == 0)
            {
                m = -1;
                return 0.0;
            }
            else
            {
                m = K;
                return maxVal;
            }
        }
        */

        public static GaussianMixtureModel KMeansInitialisation(double[] X, int K0, int lowerBound, int upperBound)
        {
            // Initialise at an even spacing
            int K1 = K0;
            List<double> means = new List<double>(K1);
            List<double> previousMeans = new List<double>(K1);
            for (int i = 0; i < K1; i++)
            {
                means.Add((i + 1.0) * (upperBound - lowerBound) / (K1 + 1.0) + lowerBound);
                previousMeans.Add(0);
            }

            int count = 0;
            int[] groupAssignments = new int[X.Length];
            int XLength = X.Length;
            List<double> totalRegionIntensity = new List<double>();
            List<int> totalRegionCount = new List<int>();
            
            while (count++ < 20)
            {
                double[] meansArray = means.ToArray();
                for(int x = 0; x < XLength; x++)
                {
                    double minDistance = Double.MaxValue;
                    double distance = 0.0;
                    for (int k = 0; k < K1; k++)
                    {
                        distance = Math.Abs(meansArray[k] - X[x]);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            groupAssignments[x] = k;
                        }
                    }
                }

                totalRegionIntensity.Clear();
                totalRegionCount.Clear();
                for (int k = 0; k < K1; k++)
                {
                    totalRegionIntensity.Add(0.0);
                    totalRegionCount.Add(0);
                }

                for (int x = 0; x < XLength; x++)
                {
                    totalRegionIntensity[groupAssignments[x]] += X[x];
                    totalRegionCount[groupAssignments[x]]++;
                }
                
                bool convergence = true;
                for (int k = 0; k < K1; k++)
                {
                    if (totalRegionCount[k] != 0)
                    {
                        means[k] = totalRegionIntensity[k] / totalRegionCount[k];
                    }
                    else
                    {
                        // Region is empty, remove it.
                        means.RemoveAt(k--);
                        totalRegionIntensity.RemoveAt(k);
                        totalRegionCount.RemoveAt(k);
                        K1--;
                        convergence = false;
                        continue;
                    }
                    
                    /*
                    if (Math.Abs(means[k] - previousMeans[k]) > 0.5)
                    {
                        convergence = false;
                        previousMeans[k] = means[k];
                    }*/
                }

                if (convergence)
                {
                    break;
                }

            }

            // Initialise GMM
            List<double> mixingWeights = new List<double>();
            List<double> sigmas = new List<double>();

            for (int k = 0; k < K1; k++)
            {
                mixingWeights.Add(totalRegionCount[k] / (double)XLength);
                //mixingWeights.Add(1.0 / K1);
                sigmas.Add(10.0);
            }

            return new GaussianMixtureModel(K1, mixingWeights.ToArray(), means.ToArray(), sigmas.ToArray());
        }
    }

    class ExpectationMaximisation
    {
        public static double Compute(double[] X, GaussianMixtureModel GMM)
        {
            // Initialisations
            int K = GMM.K;
            double[,] membership = new double[ GMM.K, X.Length];
            int count = 0;
            double? logLikelihood = null;

            while (count++ < 100)
            {
                // Expectation - recalculate membership values for all k,x pairs
                int XLength = X.Length;
                for (int x = 0; x < XLength; x++)
                {
                    double total = 0.0;
                    for (int k = 0; k < K; k++)
                    {
                        membership[k, x] = GMM.MixingWeight[k] * GMM.SingleGaussianProbabilityEM(X[x], k);
                        total += membership[k, x];
                    }
                    for (int k = 0; k < K; k++)
                        membership[k, x] /= total;
                }

                // Maximisation - recalculate GMM parameters based on the results of the expectation step
                for (int k = 0; k < K; k++)
                {
                    // Mean
                    double sumMembership = 0.0;
                    double sumMembershipX = 0.0;
                    for (int x = 0; x < XLength; x++)
                    {
                        sumMembership += membership[k, x];
                        sumMembershipX += membership[k, x] * X[x];
                    }
                    GMM.Mean[k] = sumMembershipX / sumMembership;

                    // Sigma
                    double Mean = GMM.Mean[k];
                    double sumMembershipXMinusMeanSquared = 0.0;
                    for (int x = 0; x < XLength; x++)
                    {
                        sumMembershipXMinusMeanSquared += membership[k, x] * Math.Pow(X[x] - Mean, 2.0);
                    }

                    // Prevent Sigma from going below 1 to avoid S.Ds of 0
                    GMM.Sigma[k] = Math.Max(Math.Sqrt(sumMembershipXMinusMeanSquared / sumMembership), 1);

                    // Mixing
                    GMM.MixingWeight[k] = sumMembership / XLength;
                }
                
                // Log likelihood
                double currentLikelihood = 1.0;
                for (int x = 0; x < XLength; x++)
                {
                    double value = X[x];
                    double total = 0.0;
                    for (int k = 0; k < K; k++)
                    {
                        total += GMM.MixingWeight[k] * GMM.SingleGaussianProbabilityEM(value, k);
                    }
                    currentLikelihood += Math.Log(total);
                }

                if (logLikelihood == null)
                    logLikelihood = currentLikelihood;
                else
                {
                    double ratio = currentLikelihood / (double)logLikelihood;
                    logLikelihood = currentLikelihood;
                    if (1 - ratio < 0.0001)
                    {
                        // Convergence
                        break;
                    }
                }
            }

            return logLikelihood == null ? Double.NaN : (double)logLikelihood;
        }
    }
}
