// -----------------------------------------------------------------------
// <copyright file="PrimaryRoot.cs" company="University of Nottingham">
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

using RootNav.Core.LiveWires;

namespace RootNav.Core.Measurement
{
    public class PrimaryRoot : RootBase
    {
        public override int Order
        {
            get
            {
                return 0;
            }
        }

        public override double PixelLength
        {
            get
            {
                return this.Spline.Length;
            }
        }

        public override double Length
        {
            get
            {
                double pixelLength = this.Spline.Length;

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

        public override double StartDistance
        {
            get
            {
                return 0;
            }
        }

        public override double PixelStartDistance
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
                return this.AngleWithParent;
            }
        }

        public override double TipAngle
        {
            get
            {
                double angle = 90 - Vector.AngleBetween(RootBase.HorizontalVector, this.TipVector);
                return angle > 180 ? angle - 360 : angle;
            }
        }

        public override double TotalAngle
        {
            get
            {
                double angle = 90 - Vector.AngleBetween(RootBase.HorizontalVector, this.TotalVector);
                return angle > 180 ? angle - 360 : angle;
            }
        }

        public override double AngleWithParent
        {
            get
            {
                double angle = 90 - Vector.AngleBetween(RootBase.HorizontalVector, this.EmergenceVector);
                return angle > 180 ? angle - 360 : angle;
            }
        }

        public override Vector EmergenceVector
        {
            get
            {
                Point start, end;
                if (this.AngleMaximumDistance > this.Spline.Length)
                {
                    double newMinDistanceOffset = this.AngleMaximumDistance - this.Spline.Length;
                    start = this.Spline.Start;
                    end = this.Spline.GetPoint(this.Spline.GetPositionReference(this.Spline.Length));
                }
                else
                {
                    start = this.Spline.Start;
                    end = this.Spline.GetPoint(this.Spline.GetPositionReference(this.AngleMaximumDistance));
                }
                return end - start;
            }
        }

        public override Vector TipVector
        {
            get
            {
                double tipDistance = this.Spline.SampledPointsLengths.Last();
                double innerDistance = Math.Max(0, tipDistance - this.TipAngleDistance);

                Point innerPoint = this.Spline.GetPoint(this.Spline.GetPositionReference(innerDistance));
                Point tipPoint = this.Spline.GetPoint(this.Spline.GetPositionReference(tipDistance));

                return tipPoint - innerPoint;
            }
        }

        public override Point InnerTipPoint
        {
            get
            {
                return Spline.GetPoint(Spline.GetPositionReference(Math.Max(0, this.PixelLength - this.TipAngleDistance)));
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
                return this.Spline.GetPoint(this.Spline.GetPositionReference(Math.Min(this.Spline.Length, this.AngleMaximumDistance)));
            }
        }
    }
}
