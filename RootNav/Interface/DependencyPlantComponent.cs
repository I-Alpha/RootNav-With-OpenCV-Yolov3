using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections;
using System.Linq;

namespace RootNav.Interface
{
    public class DependencyPlantComponent : DependencyObject
    {
        public static readonly DependencyProperty IsHighlightedProperty =
       DependencyProperty.Register("IsHighlighted", typeof(bool), typeof(DependencyPlantComponent), new PropertyMetadata(false));

        public bool IsHighlighted
        {
            get
            {
                return (bool)GetValue(IsHighlightedProperty);
            }
            set
            {
                SetValue(IsHighlightedProperty, value);
            }
        }

        public static readonly DependencyProperty IsSelectedProperty =
         DependencyProperty.Register("IsSelected", typeof(bool), typeof(DependencyPlantComponent), new PropertyMetadata(false));

        public double UnitConversionFactor { get; set; }

        public bool IsSelected
        {
            get
            {
                return (bool)GetValue(IsSelectedProperty);
            }
            set
            {
                SetValue(IsSelectedProperty, value);
            }
        }
    }
}
