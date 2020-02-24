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
    public class AnimatedBorder : Border
    {
        public bool IsHidden { get; set; }

        static AnimatedBorder()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedBorder),
                new FrameworkPropertyMetadata(typeof(AnimatedBorder)));
        }

        public AnimatedBorder()
            : base()
        {
            this.LayoutTransform = new ScaleTransform(1, 0);
        }

        public void Show()
        {
            double time = 0.2;

            this.IsHidden = false;

            ScaleTransform scaleTransform = new ScaleTransform(1, 0);
            this.LayoutTransform = scaleTransform;

            Duration mytime = new Duration(TimeSpan.FromSeconds(time));
            Storyboard sb = new Storyboard();

            // Scale
            DoubleAnimation scaleAnimation = new DoubleAnimation(0, 1, mytime);
            sb.Children.Add(scaleAnimation);

            Storyboard.SetTarget(scaleAnimation, this);
            Storyboard.SetTargetProperty(scaleAnimation,
                new PropertyPath("LayoutTransform.(ScaleTransform.ScaleY)"));

            DoubleAnimationUsingKeyFrames opacityAnimation = new DoubleAnimationUsingKeyFrames();
            opacityAnimation.Duration = TimeSpan.FromSeconds(time);
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0))));
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.05))));

            Storyboard.SetTarget(opacityAnimation, this);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(AnimatedBorder.OpacityProperty));
            sb.Children.Add(opacityAnimation);

            ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();
            objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new Thickness(0, 4, 4, 0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0))));
            Storyboard.SetTarget(objectAnimation, this);
            Storyboard.SetTargetProperty(objectAnimation, new PropertyPath(AnimatedBorder.MarginProperty));
            sb.Children.Add(objectAnimation);

            sb.Begin();
        }

        public void Hide()
        {
            double time = 0.2;
            this.IsHidden = true;

            ScaleTransform scaleTransform = new ScaleTransform(1, 1);
            this.LayoutTransform = scaleTransform;

            Duration mytime = new Duration(TimeSpan.FromSeconds(time));
            Storyboard sb = new Storyboard();

            DoubleAnimation scaleAnimation = new DoubleAnimation(1, 0, mytime);
            sb.Children.Add(scaleAnimation);

            Storyboard.SetTarget(scaleAnimation, this);
            Storyboard.SetTargetProperty(scaleAnimation,
                new PropertyPath("LayoutTransform.(ScaleTransform.ScaleY)"));

            ObjectAnimationUsingKeyFrames objectAnimation = new ObjectAnimationUsingKeyFrames();
            objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(new Thickness(0), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(time))));
            Storyboard.SetTarget(objectAnimation, this);
            Storyboard.SetTargetProperty(objectAnimation, new PropertyPath(AnimatedBorder.MarginProperty));
            sb.Children.Add(objectAnimation);

            sb.Begin();
        }

    }
}