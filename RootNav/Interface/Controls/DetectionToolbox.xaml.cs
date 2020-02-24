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
    public partial class DetectionToolbox : UserControl
    {
        
        #region Control Mode Enum
        public enum RootTerminalControlMode
        {
            None,
            AddPrimary,
            AddSource,
            AddLateral,
            RemoveTerminal,
        }
        #endregion

        # region Dependency Properties
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(DetectionToolbox.RootTerminalControlMode), typeof(DetectionToolbox), new PropertyMetadata(DetectionToolbox.RootTerminalControlMode.None));

        public DetectionToolbox.RootTerminalControlMode Mode
        {
            get
            {
                return (DetectionToolbox.RootTerminalControlMode)GetValue(ModeProperty);
            }
            set
            {
                SetValue(ModeProperty, value);
            }
        }
        #endregion

        public DetectionToolbox()
        {
            InitializeComponent();
        }

        #region Toggle Buttons
        public void UncheckToggleButtons(ToggleButton exclude)
        {
            foreach (ToggleButton control in this.MainGrid.Children.OfType<ToggleButton>())
            {
                if (control == this.snapToTipsCheckBox)
                {
                    continue;
                }

                if (exclude == null || control != exclude)
                {
                    control.IsChecked = false;
                }
            }

            CheckForNoControl();
        }

        private void RootToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton source = e.Source as ToggleButton;

            if (source != null)
            {
                UncheckToggleButtons(source);
            }

            if (source == this.AddRootSourceToggleButton)
            {
                this.Mode = RootTerminalControlMode.AddSource;
            }
            else if (source == this.AddPrimaryToggleButton)
            {
                this.Mode = RootTerminalControlMode.AddPrimary;
            }
            else if (source == this.AddLateralToggleButton)
            {
                this.Mode = RootTerminalControlMode.AddLateral;
            }
            else if (source == this.RemoveRootTerminalToggleButton)
            {
                this.Mode = RootTerminalControlMode.RemoveTerminal;
            }
        }

        private void RootToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckForNoControl();
        }

        private void CheckForNoControl()
        {
            bool noneChecked = true;
            foreach (ToggleButton control in this.MainGrid.Children.OfType<ToggleButton>())
            {
                if ((bool)control.IsChecked)
                {
                    noneChecked = true;
                    break;
                }
            }

            if (noneChecked)
            {
                this.Mode = DetectionToolbox.RootTerminalControlMode.None;
            }
        }
        #endregion

        private void AnalysePrimaryRootsButton_Click(object sender, RoutedEventArgs e)
        {
            RootNav.Interface.Windows.MainWindow.GetMainWindowParent(this).AnalysePrimaryRoots();
        }

        private void AnalyseLateralRootsButton_Click(object sender, RoutedEventArgs e)
        {
            RootNav.Interface.Windows.MainWindow.GetMainWindowParent(this).AnalyseLateralRoots();
        }

        private void TipDetectionButton_Click(object sender, RoutedEventArgs e)
        {
            RootNav.Interface.Windows.MainWindow.GetMainWindowParent(this).BeginTipDetection();
        }
    }
}
