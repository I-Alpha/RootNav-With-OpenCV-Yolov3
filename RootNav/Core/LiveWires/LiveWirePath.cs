using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace RootNav.Core.LiveWires
{
    public class LiveWirePath
    {
        private int sourceIndex = 0;

        public int SourceIndex
        {
            get { return sourceIndex; }
            set { sourceIndex = value; }
        }

        private List<int> indices = new List<int>();

        public List<int> Indices
        {
            get { return indices; }
            set { indices = value; }
        }

        private List<Point> intermediatePoints = new List<Point>();

        public List<Point> IntermediatePoints
        {
            get { return intermediatePoints; }
            set { intermediatePoints = value; }
        }

        private List<Point> path = null;

        public List<Point> Path
        {
            get { return path; }
            set { path = value; }
        }

        public LiveWirePath(int source, List<Point> path, List<int> indices)
        {
            this.sourceIndex = source;
            this.path = path;
            this.indices = indices;
        }
    }


    public class LiveWirePrimaryPath : LiveWirePath
    {
        private int tipIndex = 0;

        public int TipIndex
        {
            get { return tipIndex; }
            set { tipIndex = value; }
        }

        public LiveWirePrimaryPath(int source, int tip, List<Point> path, List<int> indices)
            : base(source, path, indices)
        {
            this.tipIndex = tip;
        }
    }

    public class LiveWireLateralPath : LiveWirePath
    {
        private TargetPathPoint targetPoint;

        public TargetPathPoint TargetPoint
        {
            get { return targetPoint; }
            set { targetPoint = value; }
        }

        public LiveWireLateralPath(int source, TargetPathPoint targetPoint, List<Point> path, List<int> indices)
            : base(source, path, indices)
        {
            this.targetPoint = targetPoint;
        }
    }
}
