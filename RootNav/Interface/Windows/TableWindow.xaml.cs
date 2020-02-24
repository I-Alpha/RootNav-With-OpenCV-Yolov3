using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Data;

using RootNav.Core.Measurement;

namespace RootNav.Interface.Windows
{
    /// <summary>
    /// Interaction logic for MeasurementsWindow.xaml
    /// </summary>
    public partial class TableWindow : Window
    {
        #region Variables
        private DataTable data;

        public DataTable Data
        {
            set
            {
                data = value;
            }
        }
 
        private List<String> columns = new List<string>();

        private ContextMenu headerContextMenu;

        public ContextMenu HeaderContextMenu
        {
            get { return headerContextMenu; }
            set { headerContextMenu = value; }
        }
        #endregion

        public TableWindow()
        {
            InitializeComponent();
        }

        #region Context Menu and Column Generation
        private void GenerateContextMenu()
        {
            System.Windows.Controls.ContextMenu cu = new ContextMenu();
            foreach (DataGridColumn col in this.measurementsView.Columns)
            {
                MenuItem mu = new MenuItem();
                mu.Header = col.Header;
                mu.IsCheckable = true;
                mu.IsChecked = col.Visibility == System.Windows.Visibility.Visible;
                mu.StaysOpenOnClick = true;
                mu.Checked += new RoutedEventHandler(menu_Checked);
                mu.Unchecked += new RoutedEventHandler(menu_Unchecked);
                cu.Items.Add(mu);
            }
            HeaderContextMenu = cu;
        }

        void menu_Checked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                this.measurementsView.Columns[this.HeaderContextMenu.Items.IndexOf(menuItem)].Visibility = System.Windows.Visibility.Visible;
            }
        }

        void menu_Unchecked(object sender, RoutedEventArgs e)
        {
            int visibleCount = 0;
            foreach (DataGridColumn dgc in this.measurementsView.Columns)
            {
                if (dgc.Visibility == System.Windows.Visibility.Visible)
                    visibleCount++;
            }

            if (visibleCount <= 1)
            {
                (sender as MenuItem).IsChecked = true;
                e.Handled = true;
                return;
            }

            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                this.measurementsView.Columns[this.HeaderContextMenu.Items.IndexOf(menuItem)].Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DependencyObject depObj = (DependencyObject)e.OriginalSource;

            while (depObj != null && !(depObj is System.Windows.Controls.DataGridRow))
            {
                depObj = VisualTreeHelper.GetParent(depObj);
            }

            if (depObj is System.Windows.Controls.DataGridRow)
            {
                DataGridRow currentRow = depObj as System.Windows.Controls.DataGridRow;
                int index = currentRow.GetIndex();

                if (index == 0)
                    currentRow.ContextMenu = HeaderContextMenu;
            }
        }

        #endregion


        public DataGrid View
        {
            get
            {
                return this.measurementsView;
            }
        }

        /*
        #region OnClosing
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
            this.Hide();
        }
        #endregion
         * 
         * */
    }
}

