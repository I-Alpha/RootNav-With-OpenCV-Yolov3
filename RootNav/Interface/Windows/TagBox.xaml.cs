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
using System.Windows.Shapes;

namespace RootNav.Interface.Windows
{
    /// <summary>
    /// Interaction logic for TagBox.xaml
    /// </summary>
    public partial class TagBox : Window
    {
        public String Text
        {
            get
            {
                return this.tagTextbox.Text;
            }
        }

        public bool Cancelled { get; set; }

        public TagBox(string fileName)
        {
            InitializeComponent();
            if (fileName != null && fileName != "")
            {
                this.tagTextbox.Text = fileName;
                this.tagTextbox.SelectAll();
            }
            this.tagTextbox.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Cancelled = true;
            this.DialogResult = null;
            this.Close();
        }

        private void tagTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Submit
                this.DialogResult = true;
                this.Close();
            }
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
