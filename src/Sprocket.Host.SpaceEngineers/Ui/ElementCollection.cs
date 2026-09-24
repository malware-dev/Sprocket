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

        /// <summary>Adds <paramref name="child" /> at the end and adopts it, moving it from wherever it was.</summary>
        public void Add(Element child)
        {
            Insert(_items.Count, child);
        }

        /// <summary>Puts <paramref name="child" /> at <paramref name="index" /> and adopts it, moving it from wherever it was.</summary>
        /// <remarks>Throws, leaving everything as it was, when the owner cannot place the child as its placement asks.</remarks>
        public void Insert(int index, Element child)
        {
            if (child == null || child == _owner)
                return;

            _owner.Admit(child, child.Placement);

            if (child.Holder != null)
            {
                if (child.Holder == this && _items.IndexOf(child) < index)
                    index--;

                child.Holder.Remove(child);
            }

            if (index < 0)
                index = 0;
            if (index > _items.Count)
                index = _items.Count;

            _items.Insert(index, child);
            child.Parent = _owner;
            child.Holder = this;
            _owner.Joined(child, index);

            // A child never measured is already dirty, so its own invalidation stops at itself and never
            // reaches the owner.
            child.InvalidateMeasure();
            _owner.InvalidateMeasure();
        }

        /// <summary>Where <paramref name="child" /> is, or -1 when it is not here.</summary>
        public int IndexOf(Element child)
        {
            return _items.IndexOf(child);
        }

        /// <summary>Takes <paramref name="child" /> out and orphans it, and says whether it was here.</summary>
        public bool Remove(Element child)
        {
            if (child == null)
                return false;

            var index = _items.IndexOf(child);
            if (index < 0)
                return false;

            _items.RemoveAt(index);
            child.Parent = null;
            child.Holder = null;
            _owner.Released(child, index);
            _owner.InvalidateMeasure();
            return true;
        }

        /// <summary>Takes every child out and orphans them.</summary>
        public void Clear()
        {
            for (var i = _items.Count - 1; i >= 0; i--)
            {
                var child = _items[i];
                _items.RemoveAt(i);
                child.Parent = null;
                child.Holder = null;
                _owner.Released(child, i);
            }

            _owner.InvalidateMeasure();
        }

        /// <summary>The children in order, through the list's own struct enumerator, which a foreach walks without allocating.</summary>
        public List<Element>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
