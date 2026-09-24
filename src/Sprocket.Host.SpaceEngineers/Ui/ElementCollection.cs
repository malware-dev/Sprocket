using System.Collections;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>The children of one element.</summary>
    public sealed class ElementCollection : IEnumerable<Element>
    {
        readonly List<Element> _items = new List<Element>();
        readonly Element _owner;

        /// <summary>An empty collection whose members become children of <paramref name="owner" />.</summary>
        internal ElementCollection(Element owner)
        {
            _owner = owner;
        }

        /// <summary>How many children there are.</summary>
        public int Count => _items.Count;

        /// <summary>The child at <paramref name="index" />.</summary>
        public Element this[int index] => _items[index];

        IEnumerator<Element> IEnumerable<Element>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>Adds <paramref name="child" /> and adopts it. A child already somewhere else moves.</summary>
        public void Add(Element child)
        {
            if (child == null || child == _owner)
                return;

            child.Holder?.Remove(child);

            _items.Add(child);
            child.Parent = _owner;
            child.Holder = this;

            // A child never measured is already dirty, so its own invalidation stops at itself and never
            // reaches the owner.
            child.InvalidateMeasure();
            _owner.InvalidateMeasure();
        }

        /// <summary>Takes <paramref name="child" /> out and orphans it, and says whether it was here.</summary>
        public bool Remove(Element child)
        {
            if (child == null || !_items.Remove(child))
                return false;

            child.Parent = null;
            child.Holder = null;
            _owner.InvalidateMeasure();
            return true;
        }

        /// <summary>Takes every child out and orphans them.</summary>
        public void Clear()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                _items[i].Parent = null;
                _items[i].Holder = null;
            }

            _items.Clear();
            _owner.InvalidateMeasure();
        }

        /// <summary>The children in order, through the list's own struct enumerator, which a foreach walks without allocating.</summary>
        public List<Element>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}