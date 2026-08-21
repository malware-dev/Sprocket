using System.Collections;
using System.Collections.Generic;

namespace Phosphor
{
    /// <summary>
    /// The children of one element.
    /// <para>
    /// A list of its own rather than a bare <c>List&lt;Element&gt;</c> for one reason: adding a child is what
    /// gives it a parent, and a plain list cannot do that. Leaving it to the caller means a child that was
    /// added and never adopted, which reads correctly and is wrong.
    /// </para>
    /// </summary>
    public sealed class ElementCollection : IEnumerable<Element>
    {
        private readonly List<Element> _items = new List<Element>();
        private readonly Element _owner;

        internal ElementCollection(Element owner)
        {
            _owner = owner;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public Element this[int index]
        {
            get { return _items[index]; }
        }

        /// <summary>Adds <paramref name="child" /> and adopts it. A child already somewhere else moves.</summary>
        public void Add(Element child)
        {
            if (child == null || child == _owner)
                return;

            if (child.Holder != null)
                child.Holder.Remove(child);

            _items.Add(child);
            child.Parent = _owner;
            child.Holder = this;
        }

        public bool Remove(Element child)
        {
            if (child == null || !_items.Remove(child))
                return false;

            child.Parent = null;
            child.Holder = null;
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Parent = null;
                _items[i].Holder = null;
            }

            _items.Clear();
        }

        public List<Element>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator<Element> IEnumerable<Element>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
