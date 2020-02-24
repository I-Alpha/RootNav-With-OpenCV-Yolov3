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
    public class SlideBorder : Border
    {
        public static readonly RoutedEvent HideEvent = EventManager.RegisterRoutedEvent(
            "Hide", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SlideBorder));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler Hide
        {
            add { AddHandler(HideEvent, value); }
            remove { RemoveHandler(HideEvent, value); }
        }

        public static readonly RoutedEvent ShowEvent = EventManager.RegisterRoutedEvent(
            "Show", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SlideBorder));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler Show
        {
            add { AddHandler(ShowEvent, value); }
            remove { RemoveHandler(ShowEvent, value); }
        }


        public static readonly DependencyProperty IsHiddenProperty =
            DependencyProperty.Register("IsHidden", typeof(bool), typeof(SlideBorder), new PropertyMetadata(false));

        public bool IsHidden
        {
            get
            {
                return (bool)GetValue(IsHiddenProperty);
            }
            set
            {
                SetValue(IsHiddenProperty, value);
            }
        }

        static SlideBorder()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SlideBorder),
                new FrameworkPropertyMetadata(typeof(SlideBorder)));
        }

        public SlideBorder()
            : base()
        {
            this.LayoutTransform = new TranslateTransform(0, 0);
        }

        public void BeginHide()
        {
            this.RaiseEvent(new RoutedEventArgs(HideEvent, this));
        }

        public void BeginShow()
        {
            this.RaiseEvent(new RoutedEventArgs(ShowEvent, this));
        }

    }
}