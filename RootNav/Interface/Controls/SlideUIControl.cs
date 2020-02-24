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
using RootNav.Interface.Windows;

namespace RootNav.Interface.Controls
{
    public class SlideUIControl : System.Windows.Controls.UserControl
    {
        public SlideUIControl()
        {
            this.RenderTransform = new TranslateTransform(0, 0);
        }

        public void Hide(bool reverse)
        {
            DoubleAnimation transformAnimation
                = new DoubleAnimation();

            if (!reverse)
            {
                transformAnimation.From = 0;
                transformAnimation.To = -250;
            }
            else
            {
                transformAnimation.From = 0;
                transformAnimation.To = 250;
            }

            transformAnimation.Duration
              = new Duration(new TimeSpan(0, 0, 0, 0, 200));

            Storyboard.SetTarget(transformAnimation, this.RenderTransform as TranslateTransform);
            Storyboard.SetTargetProperty(transformAnimation, new PropertyPath(TranslateTransform.XProperty));

            Storyboard hideStoryboard = new Storyboard();
            hideStoryboard.Children.Add(transformAnimation);

            ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();
            objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            Storyboard.SetTargetName(objectAnimation, this.Name);
            Storyboard.SetTargetProperty(objectAnimation, new PropertyPath(UIElement.VisibilityProperty));

            hideStoryboard.Children.Add(objectAnimation);

            this.BeginStoryboard(hideStoryboard);
            //transform.BeginAnimation(TranslateTransform.XProperty, hideStoryboard);
        }

        public void Show(bool reverse)
        {
            DoubleAnimation transformAnimation
                = new DoubleAnimation();

            if (!reverse)
            {
                transformAnimation.From = 250;
                transformAnimation.To = 0;
            }
            else
            {
                transformAnimation.From = -250;
                transformAnimation.To = 0;
            }
            transformAnimation.Duration
              = new Duration(new TimeSpan(0, 0, 0, 0, 0));

            TranslateTransform transform
              = (TranslateTransform)this.RenderTransform;

            Storyboard.SetTarget(transformAnimation, this.RenderTransform as TranslateTransform);
            Storyboard.SetTargetProperty(transformAnimation, new PropertyPath(TranslateTransform.XProperty));

            Storyboard showStoryboard = new Storyboard();
            showStoryboard.Children.Add(transformAnimation);

            ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();
            objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Visible, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            Storyboard.SetTargetName(objectAnimation, this.Name);
            Storyboard.SetTargetProperty(objectAnimation, new PropertyPath(UIElement.VisibilityProperty));

            showStoryboard.Children.Add(objectAnimation);

            this.BeginStoryboard(showStoryboard);
        }
    }
}
