﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Windows;
using RootNav.Data;

namespace RootNav.Measurement
{
    public class AngleProfileHandler : MeasurementHandler
    {
        private static int PixelResolution = 10;
        private static int K = 4;
        private Random random = new Random();

        public override string Name
        {
            get { return "Angle Profile"; }
        }

        public override MeasurementType Measures
        {
            get { return MeasurementType.Root; }
        }

        public override bool ReturnsSingleItem
        {
            get { return false; }
        }

        public override object MeasureRoot(RootInfo root, RootInfo parent = null)
        {
            List<object> distances = new List<object>();
            List<object> angles = new List<object>();

            // Obtain the curvature profile at the specified resolution
            double rootLength = root.Spline.Length;
            for (int i = K; i < rootLength - K; i += PixelResolution)
            {
                // Obtain points i - k, i, i + k
                Point p0 = root.Spline.GetPoint(root.Spline.GetPositionReference(i - K));
                Point p1 = root.Spline.GetPoint(root.Spline.GetPositionReference(i + K));

                double angle = 90 - Vector.AngleBetween(new Vector(1, 0), p1 - p0);
                angle = Math.Round(angle > 180 ? angle - 360 : angle, 2);

                distances.Add(i);
                angles.Add(angle);
            }

            List<List<object>> data = new List<List<object>>() { distances, angles };
            data[0].Insert(0, "Distance");
            data[1].Insert(0, "");

            return data;
        }
    }
}
