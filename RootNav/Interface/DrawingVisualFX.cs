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
using System.IO;
using System.Windows.Media.Effects;

using RootNav.Core.MixtureModels;
using RootNav.Interface.Controls;
using RootNav.Core.LiveWires;
using RootNav.Core.DataStructures;
using RootNav.Core.Imaging;
using RootNav.IO;
using RootNav.Core.Measurement;

namespace RootNav.Interface
{
    public class DrawingVisualFx : DrawingVisual
    {
        public static readonly DependencyProperty EffectProperty =
            DependencyProperty.Register("Effect", typeof(Effect), typeof(DrawingVisualFx),
                                        new FrameworkPropertyMetadata(null,
                                                                      (FrameworkPropertyMetadataOptions.AffectsRender),
                                                                      new PropertyChangedCallback(OnEffectChanged)));
        public new Effect Effect
        {
            get { return (Effect)GetValue(EffectProperty); }
            set { SetValue(EffectProperty, value); }
        }

        private static void OnEffectChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            DrawingVisualFx drawingVisualFx = o as DrawingVisualFx;
            if (drawingVisualFx != null)
            {
                drawingVisualFx.setMyProtectedVisualEffect((Effect)e.NewValue);

            }
        }

        private void setMyProtectedVisualEffect(Effect effect)
        {
            VisualEffect = effect;
        }
    }  
}
