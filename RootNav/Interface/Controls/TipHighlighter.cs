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
using System.Windows.Media.Animation;

namespace RootNav.Interface.Controls
{
    class TipHighlighter : Control
    {
        static TipHighlighter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TipHighlighter), new FrameworkPropertyMetadata(typeof(TipHighlighter)));
        }
    }
}
