using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace RootNav.Core.LiveWires
{
    public class RootTerminalCollection : IEnumerable<RootTerminal>
    {
        private List<RootTerminal> terminals = new List<RootTerminal>();

        private List<Tuple<RootTerminal, RootTerminal>> terminalLinks = new List<Tuple<RootTerminal, RootTerminal>>();

        public List<Tuple<RootTerminal, RootTerminal>> TerminalLinks
        {
            get { return terminalLinks; }
            set { terminalLinks = value; }
        }
        private RootTerminal previousSource = null;

        public void Add(Point point, TerminalType type, bool createLink)
        {
            RootTerminal terminal = new RootTerminal(point, type);

            if (type == TerminalType.Source)
            {
                previousSource = terminal;
            }
            else if (type == TerminalType.Primary && createLink && previousSource != null)
            {
                terminalLinks.Add(new Tuple<RootTerminal, RootTerminal>(previousSource, terminal));
            }
            else
            {
                previousSource = null;
            }
            
            this.terminals.Add(terminal);
        }

        public RootTerminal this[int i]
        {
            get
            {
                return this.terminals[i];
            }
        }

        public int Count
        {
            get
            {
                return this.terminals.Count;
            }
        }

        public int IndexOf(RootTerminal t)
        {
            return this.terminals.IndexOf(t);
        }

        public void RemoveAt(int index)
        {
            RootTerminal removedTerminal = terminals[index];
            terminals.RemoveAt(index);

            if (removedTerminal.Type == TerminalType.Source)
            {
                for (int i = terminalLinks.Count - 1; i >= 0; i--)
                {
                    var pair = terminalLinks[i];
                    if (pair.Item1 == removedTerminal)
                    {
                        terminalLinks.RemoveAt(i);
                    }
                }
            }
            else
            {
                for (int i = terminalLinks.Count - 1; i >= 0; i--)
                {
                    var pair = terminalLinks[i];
                    if (pair.Item2 == removedTerminal)
                    {
                        terminalLinks.RemoveAt(i);
                    }
                }
            }
        }

        public IEnumerable<RootTerminal> Primaries
        {
            get { return (from terminal in terminals where terminal.Type == TerminalType.Primary select terminal); }
        }

        public IEnumerable<RootTerminal> Laterals
        {
            get { return (from terminal in terminals where terminal.Type == TerminalType.Lateral select terminal); }
        }

        public IEnumerable<RootTerminal> Sources
        {
            get { return (from terminal in terminals where terminal.Type == TerminalType.Source select terminal); }
        }

        public IEnumerable<RootTerminal> Undefined
        {
            get { return (from terminal in terminals where terminal.Type == TerminalType.Undefined select terminal); }
        }

        public IEnumerable<RootTerminal> UnlinkedPrimaries
        {
            get
            {
                foreach (RootTerminal terminal in this.Primaries)
                {
                    bool found = false;
                    foreach (var linkPair in this.TerminalLinks)
                    {
                        if (linkPair.Item2 == terminal)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        yield return terminal;
                    }
                }
            }
        }

        public IEnumerable<RootTerminal> UnlinkedSources
        {
            get
            {
                foreach (RootTerminal terminal in this.Sources)
                {
                    bool found = false;
                    foreach (var linkPair in this.TerminalLinks)
                    {
                        if (linkPair.Item1 == terminal)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        yield return terminal;
                    }
                }
            }
        }

        public IEnumerator<RootTerminal> GetEnumerator()
        {
            return this.terminals.GetEnumerator();
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.terminals.GetEnumerator();
        }
    }

    public class RootTerminal
    {
        private Point position;

        public Point Position
        {
            get { return position; }
            set { position = value; }
        }

        private TerminalType type;

        public TerminalType Type
        {
            get { return type; }
            set { type = value; }
        }

        public RootTerminal(Point position, TerminalType type)
        {
            this.position = position;
            this.type = type;
        }
    }

    public enum TerminalType
    {
        Source, Primary, Lateral, Undefined
    }
}
