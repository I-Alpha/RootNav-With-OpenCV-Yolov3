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
using RootNav.Core.DataStructures;

namespace RootNav.Interface.Controls
{
    /// <summary>
    /// Interaction logic for DetectionToolbox.xaml
    /// </summary>
    public partial class MeasurementToolbox : UserControl
    {
        private static SolidColorBrush ConnectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF44DD44"));
        private static SolidColorBrush ConnectedBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF008800"));
        private static SolidColorBrush UnconnectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBBBBBB"));
        private static SolidColorBrush UnconnectedBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF888888"));

        public MeasurementToolbox()
        {
            InitializeComponent();
        }

        private void MeasurementButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GetMainWindowParent(this).MeasureRootSystem();
        }

        public void SetConnected(string connectionType, string source)
        {
            this.connectedBorder.Background = MeasurementToolbox.ConnectedBackground;
            this.connectedBorder.BorderBrush = MeasurementToolbox.ConnectedBorder;
            this.measurementOutputCheckbox.Visibility = System.Windows.Visibility.Visible;
            this.connectedLabel.Content = "Connected: " + connectionType;
            this.serverLabel.Content = source;
        }

        public void SetUnconnected()
        {
            this.connectedBorder.Background = MeasurementToolbox.UnconnectedBackground;
            this.connectedBorder.BorderBrush = MeasurementToolbox.UnconnectedBorder;
            this.measurementOutputCheckbox.Visibility = System.Windows.Visibility.Collapsed;
            this.connectedLabel.Content = "Not Connected";
            this.serverLabel.Content = "Click File -> Change Output Source to connect";
        }
    }
}
