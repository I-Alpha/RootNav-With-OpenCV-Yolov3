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
    public static class ControlAnimationExtensionMethods
    {
        public static void FadeIn(this UIElement targetControl)
        {
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200)));
            Storyboard.SetTarget(fadeInAnimation, targetControl);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
            Storyboard sb = new Storyboard();
            sb.Children.Add(fadeInAnimation);
            sb.Begin();
        }

        public static void FadeOut(this UIElement targetControl)
        {
            DoubleAnimation fadeInAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(200)));
            Storyboard.SetTarget(fadeInAnimation, targetControl);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
            Storyboard sb = new Storyboard();
            sb.Children.Add(fadeInAnimation);
            sb.Begin();
        }
    }
}
