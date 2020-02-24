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

namespace RootNav.Interface.Controls
{
    /// <summary>
    /// Interaction logic for PathInfoViewer.xaml
    /// </summary>
    public partial class PathInfoViewer : UserControl
    {
        public delegate void ParametersChangedEventHandler();
        public event ParametersChangedEventHandler ParametersChanged;

        public PathInfoViewer()
        {
            InitializeComponent();
        }

        public void SetData(RootNav.Core.LiveWires.LiveWireWeightDescriptor wd)
        {
            this.lengthTextblock.Text = Math.Round(wd.Length, 5).ToString();
            this.pixelLengthTextblock.Text = Math.Round(wd.PixelLength).ToString();
            this.mapWeightTextblock.Text = Math.Round(wd.MapWeight, 5).ToString();
            this.lengthWeightTextblock.Text = Math.Round(wd.Lengthweight, 5).ToString();
            this.curvatureWeightTextblock.Text = Math.Round(wd.CurvatureWeight, 5).ToString();
        }

        private void Checkboxes_Checked(object sender, RoutedEventArgs e)
        {
            if (this.ParametersChanged != null)
                this.ParametersChanged();
        } 
    }
}
