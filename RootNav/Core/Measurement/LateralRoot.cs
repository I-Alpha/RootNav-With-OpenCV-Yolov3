// -----------------------------------------------------------------------
// <copyright file="LateralRoot.cs" company="University of Nottingham">
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
    public class LateralRoot : RootBase
    {
        public override int Order
        {
            get
            {
                return this.Parent.Order + 1;
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

        public override double PixelStartDistance
        {
            get
            {
                RootBase parentRoot = this.Parent as RootBase;
                if (parentRoot == null)
                {
                    return 0.0;
                }
                else
                {
                    return parentRoot.Spline.GetLength(this.StartReference);
                }
            }
        }

        public override double StartDistance
        {
            get
            {
                RootBase parentRoot = this.Parent as RootBase;
                if (parentRoot == null || this.Order <= 0)
                {
                    return 0.0;
                }
                else
                {
                    double startLength = parentRoot.Spline.GetLength(this.StartReference);

                    if (this.UnitConversionFactor != 0)
                    {
                        startLength *= this.UnitConversionFactor;
                    }

                    return startLength;
                }
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
                return Vector.AngleBetween(this.EmergenceVector, this.ParentVector);
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
                    start = this.Spline.GetPoint(this.Spline.GetPositionReference(Math.Max(0.0, this.AngleMinimumDistance - newMinDistanceOffset)));
                    end = this.Spline.GetPoint(this.Spline.GetPositionReference(this.Spline.Length));
                }
                else
                {
                    start = this.Spline.GetPoint(this.Spline.GetPositionReference(Math.Min(this.Spline.Length, this.AngleMinimumDistance)));
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
                if (this.AngleMaximumDistance > this.Spline.Length)
                {
                    double newMinDistanceOffset = this.AngleMaximumDistance - this.Spline.Length;
                    return this.Spline.GetPoint(this.Spline.GetPositionReference(Math.Max(0.0, this.AngleMinimumDistance - newMinDistanceOffset)));
                }
                else
                {
                    return this.Spline.GetPoint(this.Spline.GetPositionReference(Math.Min(this.Spline.Length, this.AngleMinimumDistance)));

                }
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
