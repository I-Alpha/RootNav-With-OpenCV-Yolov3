using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.Core.DataStructures
{
    public class Node<Key, Value> where Key : IComparable<Key>
    {
        public Key key;
	    public Value value;

	    public int degree; // number of childern. used in the removeMinimum algorithm.
	    public bool mark;   // mark used in the decreaseKey algorithm.

	    //uint count; // total number of elements in tree, including this. For debug only
	
	    public Node<Key, Value> previous;  // pointers in a circular doubly linked list
        public Node<Key, Value> next;
        public Node<Key, Value> child; // pointer to the first child in the list of children
        public Node<Key, Value> parent;

        public Node(Key k, Value v)
        {
            this.key = k;
            this.value = v;
            this.degree = 0;
            this.mark = false;
            this.child = null;
            this.parent = null;
            previous = next = this;
        }

        public bool isSingle()
        {
            return (this == this.next);
        }

        public void Insert(Node<Key, Value> node)
        {
            if (node == null)
                return;

            // For example: given 1->2->3->4->1, insert a->b->c->d->a after node 3:
		    //	result: 1->2->3->a->b->c->d->4->1

            this.next.previous = node.previous;
            node.previous.next = this.next;
            this.next = node;
            node.previous = this;
        }

        public void Remove()
        {
            this.previous.next = this.next;
            this.next.previous = this.previous;
            this.next = this.previous = this;
        }

        public void AddChild(Node<Key, Value> node)
        {
            if (this.child == null)
                child = node;
            else
                child.Insert(node);

            node.parent = this;
            node.mark = false;
            this.degree++;
        }

        public void RemoveChild(Node<Key, Value> node)
        {
            if (node.parent != this)
                throw new InvalidOperationException("Cannot remove child from a node that is not it's parent");
            if (node.isSingle())
            {
                if (this.child != node)
                    throw new InvalidOperationException("Cannot remove a node that is not a child");
                this.child = null;
            }
            else
            {
                if (this.child == node)
                    this.child = node.next;
                node.Remove(); // Remove from the list of children
            }
            node.parent = null;
            node.mark = false;
            this.degree--;
        }
    }

    public class FibonacciHeap <Key,Value> where Key : IComparable<Key>
    {
        Node<Key, Value> rootWithMinKey = null;
        int count = 0;

        public int Count
        {
            get { return count; }
        }
        int maxDegree = 0;

        public Node<Key, Value> Insert(Node<Key, Value> newNode)
        {
            count++;
            return this.InsertNode(newNode);
        }

        private Node<Key, Value> InsertNode(Node<Key, Value> newNode)
        {
            if (this.rootWithMinKey == null)
            {
                this.rootWithMinKey = newNode;
            }
            else
            {
                rootWithMinKey.Insert(newNode);
                if (newNode.key.CompareTo(this.rootWithMinKey.key) < 0)
                    this.rootWithMinKey = newNode;
            }
            return newNode;
        }

        public bool IsEmpty
        {
            get
            {
                return (this.count == 0);
            }
        }

        public Node<Key, Value> Minimum()
        {
            if (this.rootWithMinKey == null)
                throw new InvalidOperationException("This heap has no minimum element");
            return this.rootWithMinKey;
        }

        public void Merge(FibonacciHeap<Key, Value> other)
        {
            this.rootWithMinKey.Insert(other.rootWithMinKey);
            if (rootWithMinKey == null || (other.rootWithMinKey != null && other.rootWithMinKey.key.CompareTo(rootWithMinKey.key) < 0))
                this.rootWithMinKey = other.rootWithMinKey;
            this.count += other.count;
        }

        public void RemoveMinimum()
        {
            if (this.rootWithMinKey == null)
                throw new InvalidOperationException("Cannot remove from an empty heap");

            this.count--;

            // Phase 1: Make all of the removed root's children new roots:
            if (this.rootWithMinKey.child != null)
            {
                Node<Key, Value> c = this.rootWithMinKey.child;
                do
                {
                    c.parent = null;
                    c = c.next;
                } while (c != this.rootWithMinKey.child);
                this.rootWithMinKey.child = null;
                this.rootWithMinKey.Insert(c);
            }

            // Phase 2-a: Handle the case where we are deleting the last key
            if (this.rootWithMinKey.next == this.rootWithMinKey)
            {
                if (this.count != 0)
                    throw new Exception("Heap error: Expected 0 keys, count is " + this.count.ToString());
                this.rootWithMinKey = null;
                return;
            }

            // Phase 2: Merge any roots with the same degree
            //System.Collections.Generic.List<Node<Key, Value>> degreeRoots = new List<Node<Key, Value>>();

            int logSize = 100;//(int)Math.Log(this.Count) * 2;
            Node<Key, Value>[] degreeRoots = new Node<Key, Value>[logSize];
            
            maxDegree = 0;
            Node<Key, Value> currentPointer = this.rootWithMinKey.next;
            int currentDegree;

            do
            {
                currentDegree = currentPointer.degree;

                Node<Key, Value> current = currentPointer;
                currentPointer = currentPointer.next;
                while (degreeRoots[currentDegree] != null) // Merge two roots with the same degree
                {
                    Node<Key, Value> other = degreeRoots[currentDegree]; // Another root that has the same degree
                    // Swap if necessary
                    if (current.key.CompareTo(other.key) > 0)
                    {
                        Node<Key, Value> temp = other;
                        other = current;
                        current = temp;
                    }

                    // Now make smaller key is in current, make other the child of current
                    other.Remove();
                    current.AddChild(other);
                    degreeRoots[currentDegree] = null;
                    currentDegree++;
                }
                // Keep the current root as the first of it's degree in the degrees array
                degreeRoots[currentDegree] = current;
            } while (currentPointer != this.rootWithMinKey);

            // Phase 3: Remove the current root, and calculate the new rootWithMinKey
            this.rootWithMinKey = null;

            int newMaxDegree = 0;

            for (int d = 0; d < logSize; d++)
            {
                if (degreeRoots[d] != null)
                {
                    degreeRoots[d].next = degreeRoots[d].previous = degreeRoots[d];
                    this.InsertNode(degreeRoots[d]);
                    if (d > newMaxDegree)
                        newMaxDegree = d;
                }
            }
            maxDegree = newMaxDegree;
        }

        public void DecreaseKey(Node<Key, Value> node, Key newKey)
        {
            if (newKey.CompareTo(node.key) > 0)
                throw new InvalidOperationException("Cannot decrease a key to a greater value");
            else if (newKey.CompareTo(node.key) == 0)
                return;

            // Update the key and possibly the minkey
            node.key = newKey;

            // Check if the new key violates the min-heap property
            Node<Key, Value> parent = node.parent;
            if (parent == null)
            {
                // Node is a root, ensure the minimum root is correct
                if (newKey.CompareTo(this.rootWithMinKey.key) < 0)
                    this.rootWithMinKey = node;
                return; // Invariant not violated, nothing more to do.
            }
            else if (parent.key.CompareTo(newKey) <= 0)
                return; // Invariant not violated, nothing more to do.

            // Adjust tree using a cascading cut
            while (true)
            {
                // Remove node from parent and add into the root
                parent.RemoveChild(node);
                this.InsertNode(node);

                if (parent.parent == null)
                {
                    // Parent is now a root, nothing more to do
                    return;
                }
                else if (!parent.mark)
                {
                    // Parent is not a root, but it is not marked. Mark it then nothing else to do
                    parent.mark = true;
                    break;
                }
                else
                {
                    // Parent is not a root, and is marked. We must continue our cascading cut
                    node = parent;
                    parent = parent.parent;
                    continue;
                }
            }
        }
    }
}
