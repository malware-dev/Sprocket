using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel
{
    public abstract partial class Element
    {
        /// <summary>The children of one element.</summary>
        /// <remarks>Nested, so that it alone can set a child's parent and an element's children.</remarks>
        public sealed class Collection : IEnumerable<Element>
        {
            readonly List<Element> _items = new List<Element>();
            readonly Element _owner;

            /// <summary>An empty collection whose members become children of <paramref name="owner" />.</summary>
            /// <remarks>Throws when <paramref name="owner" /> already holds one: an element makes its children once.</remarks>
            public Collection(Element owner) : this(owner, null, null, null, null)
            {
            }

            /// <summary>
            ///     An empty collection whose members become children of <paramref name="owner" />, which is asked whether it
            ///     can place a child through <paramref name="admit" />, and told of every change through the others.
            /// </summary>
            /// <remarks>Throws when <paramref name="owner" /> already holds one: an element makes its children once.</remarks>
            public Collection(
                Element owner,
                Action<Element, IPlacement> admit,
                Action<Element, int> joined,
                Action<Element, int> released,
                Action<Element> replaced)
            {
                if (owner._contents != null)
                    throw new InvalidOperationException("An element's children are made once, by the element.");

                _owner = owner;
                owner._contents = this;
                owner._admit = admit;
                owner._joined = joined;
                owner._released = released;
                owner._replaced = replaced;
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

                if (_owner._admit != null)
                    _owner._admit(child, child.Placement);

                if (child._parent != null)
                {
                    if (child._parent == _owner && _items.IndexOf(child) < index)
                        index--;

                    child._parent._contents.Remove(child);
                }

                if (index < 0)
                    index = 0;
                if (index > _items.Count)
                    index = _items.Count;

                _items.Insert(index, child);
                child._parent = _owner;

                if (_owner._joined != null)
                    _owner._joined(child, index);

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
                Orphan(child, index);
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
                    Orphan(child, i);
                }

                _owner.InvalidateMeasure();
            }

            /// <summary>The children in order, through the list's own struct enumerator, which a foreach walks without allocating.</summary>
            public List<Element>.Enumerator GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            void Orphan(Element child, int index)
            {
                child._parent = null;

                if (_owner._released != null)
                    _owner._released(child, index);
            }
        }
    }
}
