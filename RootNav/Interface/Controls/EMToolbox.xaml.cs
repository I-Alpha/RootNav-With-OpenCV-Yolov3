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
using System.Windows.Controls.Primitives;

using RootNav.Interface.Windows;

using RootNav.Core.MixtureModels;

namespace RootNav.Interface.Controls
{
    /// <summary>
    /// Interaction logic for DetectionToolbox.xaml
    /// </summary>
    public partial class EMToolbox : UserControl
    {
        private bool LoadingConfig = false;

        public EMToolbox()
        {
            InitializeComponent();
        }

        private void reCalculateEMButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GetMainWindowParent(this).BeginEMFromToolbox();
        }

        private void emPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindow.GetMainWindowParent(this).EMToolboxConfigurationChanged(this.emPresetComboBox.SelectedIndex);
        }

        public void LoadPresetNames(string[] names, int initialIndex)
        {
            this.emPresetComboBox.Items.Clear();
            foreach (string name in names)
            {
                this.emPresetComboBox.Items.Add(name);
            }
            this.emPresetComboBox.Items.Add("Custom");
            emPresetComboBox.SelectedIndex = initialIndex;
        }

        public void FromEMConfiguration(EMConfiguration currentConfig)
        {
            this.LoadingConfig = true;

            this.initialCountTextBox.Text = currentConfig.InitialClassCount.ToString();
            this.maximumCountTextBox.Text = currentConfig.MaximumClassCount.ToString();
            this.expectedClassCountTextBox.Text = currentConfig.ExpectedRootClassCount.ToString();
            this.patchSizeTextBox.Text = currentConfig.PatchSize.ToString();

            this.thresholdPercentageTextBox.Text = currentConfig.BackgroundPercentage.ToString();
            this.thresholdSigmaTextBox.Text = currentConfig.BackgroundExcessSigma.ToString();

            this.weightsTextBox.Text = DoubleArrayToString(currentConfig.Weights);

            this.LoadingConfig = false;
        }

        private String DoubleArrayToString(double[] d)
        {
            String s = "";

            foreach (double i in d)
            {
                s += i.ToString() + ",";
            }

            return s.Trim(',');
        }

        private double[] StringToDoubleArray(String s)
        {
            string[] splits = s.Split(' ', ',', ';');

            if (splits.Length == 0)
            {
                return new double[0];
            }

            double[] output = new double[splits.Length];

            for (int i = 0; i < splits.Length; i++)
            {
                string sub = splits[i];
                output[i] = double.Parse(sub);
            }

            return output;
        }

        public EMConfiguration ToEMConfiguration()
        {
            EMConfiguration e = new EMConfiguration()
            {
                Name = "Custom",
                InitialClassCount = Int32.Parse(this.initialCountTextBox.Text),
                MaximumClassCount = Int32.Parse(this.maximumCountTextBox.Text),
                ExpectedRootClassCount = Int32.Parse(this.expectedClassCountTextBox.Text),
                PatchSize = Int32.Parse(this.patchSizeTextBox.Text),
                BackgroundPercentage = double.Parse(this.thresholdPercentageTextBox.Text),
                BackgroundExcessSigma = double.Parse(this.thresholdSigmaTextBox.Text),
                Weights = StringToDoubleArray(this.weightsTextBox.Text)
            };
            return e;
        }

        private void ValuesChanged(object sender, TextChangedEventArgs e)
        {
            if (!LoadingConfig)
            {
                this.emPresetComboBox.SelectedIndex = this.emPresetComboBox.Items.Count - 1;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.advancedBorder.IsHidden)
            {
                this.advancedBorder.Show();
            }
            else
            {
                this.advancedBorder.Hide();
            }
        }
    }
}
