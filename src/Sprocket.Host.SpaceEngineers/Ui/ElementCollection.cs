// Needed by one of the two projects this shared code is compiled into and not the other, so an
// inspection run against either will call it redundant. It is not.
using System.Collections;
// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>The children of one element.</summary>
    /// <remarks>
    ///     A list of its own rather than a bare <c>List&lt;Element&gt;</c> because adding a child is what
    ///     gives it a parent, and a plain list cannot do that.
    /// </remarks>
    public sealed class ElementCollection : IEnumerable<Element>
    {
        readonly List<Element> _items = new List<Element>();
        readonly Element _owner;

        internal ElementCollection(Element owner)
        {
            _owner = owner;
        }

        public int Count => _items.Count;

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

            // The child arrives dirty - it has never been measured where it now is - and the owner has to
            // hear about it, because a container's size is made of what is in it.
            child.InvalidateMeasure();
            _owner.InvalidateMeasure();
        }

        public bool Remove(Element child)
        {
            if (child == null || !_items.Remove(child))
                return false;

            child.Parent = null;
            child.Holder = null;
            _owner.InvalidateMeasure();
            return true;
        }

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

        public List<Element>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}