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

namespace RootNav
{
    /// <summary>
    /// Interaction logic for ExceptionHandlerWindow.xaml
    /// </summary>
    public partial class ExceptionHandlerWindow : Window
    {
        public ExceptionHandlerWindow()
        {
            InitializeComponent();
        }

        public void SetText(Exception e)
        {
            this.headerBox.Text = e.Message;
            this.mainBox.Text = e.StackTrace;
        }

        public void SetText(String s)
        {
            this.mainBox.Text = s;
        }
    }
}
