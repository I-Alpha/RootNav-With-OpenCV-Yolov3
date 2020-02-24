// -----------------------------------------------------------------------
// <copyright file="RootGroup.cs" company="University of Nottingham">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections;
using System.Linq;

namespace RootNav.Core.Measurement
{
    public class RootGroup : RootBase
    {
        public override int Order
        {
            get
            {
                return -1;
            }
        }

        public override double PixelLength
        {
            get
            {
                if (this.Children != null)
                {
                    return RecursiveLength(this.Children);
                }
                else
                {
                    return 0.0;
                }
            }
        }

        private double RecursiveLength(List<PlantComponent> roots)
        {
            double length = 0;
            foreach (RootBase r in roots)
            {
                length += r.PixelLength;
                if (r.Children != null)
                {
                    length += RecursiveLength(r.Children);
                }
            }
            return length;
        }

        public override double Length
        {
            get
            {
                double pixelLength = 0.0;

                if (this.Children != null)
                {
                    pixelLength = RecursiveLength(this.Children);
                }

                if (this.UnitConversionFactor != 0)
                {
                    return pixelLength * this.UnitConversionFactor;
                }
                else
                {
                    return pixelLength;
                }
            }
        }

        public override double PixelStartDistance
        {
            get
            {
                return 0.0;
            }
        }

        public override double StartDistance
        {
            get
            {
                return 0.0;
            }
        }

        public override double EmergenceAngle
        {
            get
            {
                return 0.0;
            }
        }

        public override double TipAngle
        {
            get
            {
                return 0.0;
            }
        }

        public override double TotalAngle
        {
            get
            {
                return 0.0;
            }
        }

        public override double AngleWithParent
        {
            get
            {
                return 0.0;
            }
        }

        public override Vector EmergenceVector
        {
            get
            {
                return new Vector(0, 0);
            }
        }

        public override Vector TipVector
        {
            get
            {
                return new Vector(0, 0);
            }
        }

        public override Point InnerTipPoint
        {
            get
            {
                return default(Point);
            }
        }

        public override Point MinimumAnglePoint
        {
            get
            {
                return default(Point);
            }
        }

        public override Point MaximumAnglePoint
        {
            get
            {
                return default(Point);
            }
        }
    }
}
