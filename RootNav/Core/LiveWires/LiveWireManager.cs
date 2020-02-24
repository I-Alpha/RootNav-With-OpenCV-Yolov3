using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;

namespace RootNav.Core.LiveWires
{ 
    public class LiveWireManager
    {
        public class LiveWireWorkNotCompletedException : Exception
        {
            public LiveWireWorkNotCompletedException(String message)
                : base(message)
            {
            }

            public LiveWireWorkNotCompletedException(String message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        public event ProgressChangedEventHandler ProgressChanged;
        public event RunWorkerCompletedEventHandler ProgressCompleted;

        protected List<bool> currentWorkersCompleted = null;
        protected List<int> currentWorkersProgress = null;
        protected List<LiveWireWorker> currentWorkers = null;
        
        protected void OnProgressChanged(LiveWireManager sender, ProgressChangedEventArgs args)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(sender, args);
            }
        }

        protected void OnProgressCompleted(LiveWireManager sender, RunWorkerCompletedEventArgs args)
        {
            if (this.ProgressCompleted != null)
            {
                this.ProgressCompleted(sender, args);
            }
        }

        protected void OnWorkerProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            LiveWireWorker w = sender as LiveWireWorker;

            if (w == null)
            {
                throw new LiveWireWorkNotCompletedException("Invalid worker reported a progress change");
            }

            lock (currentWorkersProgress)
            {
                currentWorkersProgress[currentWorkers.IndexOf(w)] = args.ProgressPercentage;
                int combinedProgress = 0;
                combinedProgress = currentWorkersProgress.Sum() / currentWorkersProgress.Count;


                if (currentProgress != combinedProgress)
                {
                    currentProgress = combinedProgress;
                    OnProgressChanged(this, new ProgressChangedEventArgs(currentProgress, null));
                }
            }
        }
     
        protected volatile bool workCompleted = false;
        
        public bool IsWorkCompleted
        {
            get
            {
                return this.workCompleted;
            }
        }

        protected int currentProgress;
    }

    public class LiveWirePrimaryManager : LiveWireManager
    {
        public int Width { get; set; }
        public int Height { get; set; }
   
        public Dictionary<Tuple<int, int>, List<Point>> Paths { get; set; }
        public Dictionary<Tuple<int, int>, List<int>> ControlPointIndices { get; set; }

        public LiveWireGraph Graph { get; set; }
        public RootTerminalCollection Terminals { get; set; }
        public int ThreadCount { get; set; }
        public List<LiveWirePrimaryPath> ReWorkPaths;
        public double[,] DistanceMap { get; set; }

        public void Run()
        {
            // Assign width and height and other properties
            this.Width = Graph.Width;
            this.Height = Graph.Height;

            // Create thread workers
            currentWorkers = new List<LiveWireWorker>();
            currentWorkersCompleted = new List<bool>();
            currentWorkersProgress = new List<int>();
            currentProgress = 0;
            for (int worker = 0; worker < ThreadCount; worker++)
            {
                LiveWirePrimaryWorker thread = new LiveWirePrimaryWorker();
                thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkerProgressCompleted);
                thread.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                currentWorkers.Add(thread);
                currentWorkersCompleted.Add(false);
                currentWorkersProgress.Add(0);
            }

            // Create data objects for workers
            List<List<Tuple<int, int>>> workerPointPairs = new List<List<Tuple<int, int>>>();
            for (int t = 0; t < ThreadCount; t++)
                workerPointPairs.Add(new List<Tuple<int,int>>());

            List<Tuple<int, int>> terminalPairs = new List<Tuple<int, int>>();

            foreach (RootTerminal sourceTerminal in Terminals.UnlinkedSources)
            {
                foreach (RootTerminal tipTerminal in Terminals.UnlinkedPrimaries)
                {
                    terminalPairs.Add(new Tuple<int, int>(Terminals.IndexOf(sourceTerminal), Terminals.IndexOf(tipTerminal)));
                }
            }

            if (Terminals.TerminalLinks != null && Terminals.TerminalLinks.Count > 0)
            {
                foreach (var link in Terminals.TerminalLinks)
                {
                    terminalPairs.Add(new Tuple<int, int>(Terminals.IndexOf(link.Item1), Terminals.IndexOf(link.Item2)));
                }
            }

            int currentThreadIndex = 0;
            foreach (Tuple<int, int> terminalPair in terminalPairs)
            {
                // Assign patch to a worker thread
                workerPointPairs[currentThreadIndex].Add(terminalPair);
                currentThreadIndex = (currentThreadIndex + 1) % ThreadCount;
            }
            
            // Launch workers
            for (int worker = 0; worker < currentWorkers.Count; worker++)
            {
                LiveWirePrimaryWorker w = currentWorkers[worker] as LiveWirePrimaryWorker;
                w.Mode = LiveWireWorker.WorkMode.Work;
                w.TerminalPairs = workerPointPairs[worker];
                w.TerminalCollection = Terminals;
                w.Graph = Graph;
                w.DistanceMap = DistanceMap;
                w.RunWorkerAsync();
            }
        }

        public void ReRun()
        {
            // Assign width and height and other properties
            this.Width = Graph.Width;
            this.Height = Graph.Height;

            // Create thread workers
            currentWorkers = new List<LiveWireWorker>();
            currentWorkersCompleted = new List<bool>();
            currentWorkersProgress = new List<int>();
            currentProgress = 0;
            for (int worker = 0; worker < ThreadCount; worker++)
            {
                LiveWirePrimaryWorker thread = new LiveWirePrimaryWorker();
                thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkerProgressCompleted);
                thread.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                currentWorkers.Add(thread);
                currentWorkersCompleted.Add(false);
                currentWorkersProgress.Add(0);
            }

            // Create data objects for workers
            List<List<LiveWirePrimaryPath>> pathLists = new List<List<LiveWirePrimaryPath>>();
            for (int t = 0; t < ThreadCount; t++)
                pathLists.Add(new List<LiveWirePrimaryPath>());

            int currentThreadIndex = 0;
            foreach (LiveWirePrimaryPath p in ReWorkPaths)
            {
                // Assign an index to a worker thread
                pathLists[currentThreadIndex].Add(p);
                currentThreadIndex = (currentThreadIndex + 1) % ThreadCount;
            }

            // Launch workers
            for (int worker = 0; worker < currentWorkers.Count; worker++)
            {
                LiveWirePrimaryWorker w = currentWorkers[worker] as LiveWirePrimaryWorker;
                w.Mode = LiveWireWorker.WorkMode.ReWork;
                w.Graph = Graph;
                w.TerminalCollection = Terminals;
                w.ReWorkPaths = pathLists[worker];
                w.DistanceMap = DistanceMap;
                w.RunWorkerAsync();
            }
        }

        private void OnWorkerProgressCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            LiveWirePrimaryWorker worker = sender as LiveWirePrimaryWorker;

            if (worker == null)
            {
                return;  // TODO: Handle Error
            }

            if (args.Error != null)
            {
                throw new LiveWireWorkNotCompletedException("An error occurred in a LiveWire Worker", args.Error);
            }

            this.currentWorkersCompleted[currentWorkers.IndexOf(worker)] = true;

            if (!this.currentWorkersCompleted.Contains(false))
            {
                this.Paths = new Dictionary<Tuple<int, int>, List<Point>>();
                this.ControlPointIndices = new Dictionary<Tuple<int, int>, List<int>>();
                foreach (LiveWirePrimaryWorker w in this.currentWorkers)
                {
                    foreach (KeyValuePair<Tuple<int, int>, List<Point>> kvp in w.Paths)
                    {
                        this.Paths.Add(kvp.Key, kvp.Value);
                    }

                    foreach (KeyValuePair<Tuple<int, int>, List<int>> kvp in w.ControlPointIndices)
                    {
                        this.ControlPointIndices.Add(kvp.Key, kvp.Value);
                    }
                }

                this.workCompleted = true;

                // Clear memory
                this.currentWorkers = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                base.OnProgressCompleted(this, new RunWorkerCompletedEventArgs(null, null, false));
            }
        }
    }

    public class LiveWireLateralManager : LiveWireManager
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Dictionary<int, List<Point>> Paths { get; set; }
        public Dictionary<int, List<int>> ControlPointIndices { get; set; }
        public Dictionary<int, TargetPathPoint> TargetPathIndices { get; set; }

        public LiveWireGraph Graph { get; set; }
        public RootTerminalCollection Terminals { get; set; }
        public List<LiveWirePrimaryPath> CurrentPaths { get; set; }
        public int ThreadCount { get; set; }
        public List<LiveWireLateralPath> ReWorkPaths { get; set; }
        public double[,] DistanceMap { get; set; }

        public void Run()
        {
            // Assign width and height and other properties
            this.Width = Graph.Width;
            this.Height = Graph.Height;

            // Create thread workers
            currentWorkers = new List<LiveWireWorker>();
            currentWorkersCompleted = new List<bool>();
            currentWorkersProgress = new List<int>();
            currentProgress = 0;
            for (int worker = 0; worker < ThreadCount; worker++)
            {
                LiveWireLateralWorker thread = new LiveWireLateralWorker();
                thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkerProgressCompleted);
                thread.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                currentWorkers.Add(thread);
                currentWorkersCompleted.Add(false);
                currentWorkersProgress.Add(0);
            }

            // Create data objects for workers
            List<List<int>> workerLateralIndices = new List<List<int>>();
            for (int t = 0; t < ThreadCount; t++)
                workerLateralIndices.Add(new List<int>());

            List<int> lateralIndices = new List<int>();

            foreach (RootTerminal lateralTerminal in (from terminal in Terminals where terminal.Type == TerminalType.Lateral select terminal))
            {
                lateralIndices.Add(Terminals.IndexOf(lateralTerminal));
            }

            int currentThreadIndex = 0;
            foreach (int lateral in lateralIndices)
            {
                // Assign patch to a worker thread
                workerLateralIndices[currentThreadIndex].Add(lateral);
                currentThreadIndex = (currentThreadIndex + 1) % ThreadCount;
            }

            // Dictionary of end points
            Dictionary<Int32Point, int> endPoints = new Dictionary<Int32Point, int>();
            for (int i = 0; i < CurrentPaths.Count; i++)
            {
                LiveWirePrimaryPath currentPath = CurrentPaths[i];
                foreach (Point p in currentPath.Path)
                {
                    Int32Point key = (Int32Point)p;
                    if (!endPoints.ContainsKey(key))
                    {
                        endPoints.Add(key, i);
                    }
                }
            }

            // Launch workers
            for (int worker = 0; worker < currentWorkers.Count; worker++)
            {
                LiveWireLateralWorker w = currentWorkers[worker] as LiveWireLateralWorker;
                w.LateralTerminals = workerLateralIndices[worker];
                w.Mode = LiveWireWorker.WorkMode.Work;
                w.TerminalCollection = Terminals;
                w.CurrentPathIndexes = endPoints;
                w.DistanceMap = DistanceMap;
                w.Graph = Graph;
                w.RunWorkerAsync();
            }
        }

        public void ReRun()
        {
            // Assign width and height and other properties
            this.Width = Graph.Width;
            this.Height = Graph.Height;

            // Create thread workers
            currentWorkers = new List<LiveWireWorker>();
            currentWorkersCompleted = new List<bool>();
            currentWorkersProgress = new List<int>();
            currentProgress = 0;
            for (int worker = 0; worker < ThreadCount; worker++)
            {
                LiveWireLateralWorker thread = new LiveWireLateralWorker();
                thread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OnWorkerProgressCompleted);
                thread.ProgressChanged += new ProgressChangedEventHandler(OnWorkerProgressChanged);
                currentWorkers.Add(thread);
                currentWorkersCompleted.Add(false);
                currentWorkersProgress.Add(0);
            }

            // Create data objects for workers
            List<List<LiveWireLateralPath>> workerLateralPaths = new List<List<LiveWireLateralPath>>();
            for (int t = 0; t < ThreadCount; t++)
                workerLateralPaths.Add(new List<LiveWireLateralPath>());

            int currentThreadIndex = 0;
            foreach (LiveWireLateralPath p in ReWorkPaths)
            {
                // Assign patch to a worker thread
                workerLateralPaths[currentThreadIndex].Add(p);
                currentThreadIndex = (currentThreadIndex + 1) % ThreadCount;
            }

            // Dictionary of end points
            Dictionary<Int32Point, int> endPoints = new Dictionary<Int32Point, int>();
            for (int i = 0; i < CurrentPaths.Count; i++)
            {
                LiveWirePrimaryPath currentPath = CurrentPaths[i];
                foreach (Point p in currentPath.Path)
                {
                    Int32Point key = (Int32Point)p;
                    if (!endPoints.ContainsKey(key))
                    {
                        endPoints.Add(key, i);
                    }
                }
            }

            // Launch workers
            for (int worker = 0; worker < currentWorkers.Count; worker++)
            {
                LiveWireLateralWorker w = currentWorkers[worker] as LiveWireLateralWorker;
                w.Graph = Graph;
                w.Mode = LiveWireWorker.WorkMode.ReWork;
                w.TerminalCollection = Terminals;
                w.CurrentPathIndexes = endPoints;
                w.ReWorkPaths = workerLateralPaths[worker];
                w.DistanceMap = DistanceMap;
                w.RunWorkerAsync();
            }
        }

        private void OnWorkerProgressCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            LiveWireLateralWorker worker = sender as LiveWireLateralWorker;

            if (worker == null)
            {
                return;  // TODO: Handle Error
            }

            if (args.Error != null)
            {
                throw new LiveWireWorkNotCompletedException("An error occurred in a LiveWire Worker", args.Error);
            }

            this.currentWorkersCompleted[currentWorkers.IndexOf(worker)] = true;

            if (!this.currentWorkersCompleted.Contains(false))
            {
                this.Paths = new Dictionary<int, List<Point>>();
                this.ControlPointIndices = new Dictionary<int, List<int>>();
                this.TargetPathIndices = new Dictionary<int, TargetPathPoint>();
                foreach (LiveWireLateralWorker w in this.currentWorkers)
                {
                    foreach (var kvp in w.Paths)
                    {
                        this.Paths.Add(kvp.Key, kvp.Value);
                    }

                    foreach (var kvp in w.ControlPointIndices)
                    {
                        this.ControlPointIndices.Add(kvp.Key, kvp.Value);
                    }

                    foreach (var item in w.TargetPathIndices)
                    {
                        this.TargetPathIndices.Add(item.Key, item.Value);
                    }
                }

                this.workCompleted = true;

                // Clear memory
                this.currentWorkers = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                base.OnProgressCompleted(this, new RunWorkerCompletedEventArgs(null, null, false));
            }
        }
    } 






}
