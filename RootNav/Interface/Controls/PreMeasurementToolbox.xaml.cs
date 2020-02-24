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

namespace RootNav.Interface.Controls
{
    /// <summary>
    /// Interaction logic for DetectionToolbox.xaml
    /// </summary>
    public partial class PreMeasurementToolbox : UserControl
    {
        public PreMeasurementToolbox()
        {
            InitializeComponent();
        }

        private void MeasurementButton_Click(object sender, RoutedEventArgs e)
        {
            RootNav.Interface.Windows.MainWindow.GetMainWindowParent(this).BeginMeasurementStage();
        }
    }
}
