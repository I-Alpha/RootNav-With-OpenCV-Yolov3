using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace RootNav.Core.LiveWires
{
    public class LiveWirePathCollection : IEnumerable<LiveWirePath>
    {
        public delegate void PathAddedEventHandler();

        public event PathAddedEventHandler PathAdded;

        private List<LiveWirePath> paths = new List<LiveWirePath>();

        public void Add(LiveWirePath path)
        {
            this.paths.Add(path);

            if (this.PathAdded != null)
                PathAdded();
        }

        public void Clear()
        {
            this.paths.Clear();
        }

        public void ClearLaterals()
        {
            for (int i = this.paths.Count - 1; i >= 0; i--)
            {
                if (this.paths[i] is LiveWireLateralPath)
                {
                    this.paths.RemoveAt(i);
                }
            }
        }

        public LiveWirePath this[int i]
        {
            get
            {
                return this.paths[i];
            }
        }

        public int Count
        {
            get
            {
                return this.paths.Count;
            }
        }

        public int IndexOf(LiveWirePath t)
        {
            return this.paths.IndexOf(t);
        }

        public IEnumerable<LiveWirePrimaryPath> Primaries
        {
            get { return (from path in paths where path is LiveWirePrimaryPath select path as LiveWirePrimaryPath); }
        }

        public IEnumerable<LiveWireLateralPath> Laterals
        {
            get { return (from path in paths where path is LiveWireLateralPath select path as LiveWireLateralPath); }
        }

        public IEnumerator<LiveWirePath> GetEnumerator()
        {
            return this.paths.GetEnumerator();
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.paths.GetEnumerator();
        }
    }
}
