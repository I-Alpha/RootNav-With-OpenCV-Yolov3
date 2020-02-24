using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.Core.Measurement
{
    public class RootCollection : IEnumerable<RootBase>
    {
        private List<RootBase> roots = new List<RootBase>();

        public List<RootBase> RootTree
        {
            get { return roots; }
            set { roots = value; }
        }

        public IEnumerator<RootBase> GetEnumerator()
        {
            Stack<RootBase> stack = new Stack<RootBase>();

            foreach (RootBase r in roots)
            {
                stack.Push(r);
            }

            while (stack.Count > 0)
            {
                RootBase current = stack.Pop();
                yield return current;

                foreach (PlantComponent pc in current.Children)
                {
                    stack.Push(pc as RootBase);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
