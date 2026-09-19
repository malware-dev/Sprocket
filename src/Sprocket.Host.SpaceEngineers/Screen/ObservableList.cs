using System;
using System.Collections;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// A list that says when it has changed.
    /// </summary>
    /// <remarks>
    /// Space Engineers does not whitelist <c>ObservableCollection</c> or
    /// <c>INotifyCollectionChanged</c>, which is not a reason to do without them: a
    /// list, a delegate and an event are all available, and fifty lines buys the same
    /// thing. The whitelist bounds the parts, not what can be built from them.
    ///
    /// The event is deliberately coarse - "this list is different now" rather than
    /// "item 3 moved to 5". Sprocket rebuilds whole lists anyway (a search narrows
    /// them, a selection re-derives them), so per-item detail would be invented
    /// precision that every target then had to handle.
    /// </remarks>
    public sealed class ObservableList<T> : IEnumerable<T>
    {
        readonly List<T> _items = new List<T>();

        /// <summary>Raised after any change to the contents.</summary>
        /// <remarks>
        /// Fired while core is still mid-update, so a watcher that rebuilds controls
        /// from here reads half-written data. <see cref="Revision"/> exists for that
        /// reason: a target can notice the change at a moment of its own choosing
        /// instead of being dragged into core's.
        /// </remarks>
        public event Action Changed;

        /// <summary>
        /// Bumped on every change, so a watcher can tell whether it has seen the
        /// current contents without being called back.
        /// </summary>
        public int Revision { get; private set; }

        public int Count => _items.Count;

        public T this[int index]
        {
            get { return _items[index]; }
            set
            {
                _items[index] = value;
                Announce();
            }
        }

        public void Add(T item)
        {
            _items.Add(item);
            Announce();
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            Announce();
        }

        public bool Remove(T item)
        {
            if (!_items.Remove(item))
                return false;

            Announce();
            return true;
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
            Announce();
        }

        public void Clear()
        {
            if (_items.Count == 0)
                return;

            _items.Clear();
            Announce();
        }

        /// <summary>
        /// Replaces the contents in one go, announcing once.
        /// </summary>
        /// <remarks>
        /// The usual way lists change here: a search narrows them, a selection
        /// re-derives them. Clearing and adding one at a time would wake every
        /// watcher once per item and make them redraw a list that is still half
        /// built.
        /// </remarks>
        public void Reset(IEnumerable<T> items)
        {
            _items.Clear();
            if (items != null)
                _items.AddRange(items);

            Announce();
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        void Announce()
        {
            Revision++;

            var handler = Changed;
            if (handler != null)
                handler();
        }
    }
}
