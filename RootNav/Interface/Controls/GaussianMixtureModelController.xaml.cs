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
    /// Interaction logic for GaussianMixtureModelController.xaml
    /// </summary>
    public partial class GaussianMixtureModelController : UserControl
    {
        public delegate void ParametersChangedEventHandler();
        public event ParametersChangedEventHandler ParametersChanged;

        public GaussianMixtureModelController()
        {
            InitializeComponent();
        }

        private void Checkboxes_Checked(object sender, RoutedEventArgs e)
        {
            if (this.ParametersChanged != null)
                this.ParametersChanged();
        } 
    }
}
