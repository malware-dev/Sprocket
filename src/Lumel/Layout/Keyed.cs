using System;
using System.Collections.Generic;

namespace Lumel.Layout
{
    /// <summary>The children of one container, one for each key, kept from one showing to the next.</summary>
    /// <remarks>
    ///     A key already shown keeps its element, and only an element out of place is moved, so a list that gains
    ///     or loses one entry adds or removes one child: every other one keeps what it holds, the pointer resting
    ///     on it and a press under way included, and nothing is made again. The container's children are this
    ///     one's alone to change.
    /// </remarks>
    public sealed class Keyed<TKey, TElement> where TElement : Element
    {
        readonly Element.Collection _children;
        readonly Func<TKey, TElement> _make;
        readonly Dictionary<TKey, Entry> _kept = new Dictionary<TKey, Entry>();
        readonly List<TKey> _gone = new List<TKey>();

        int _showing;

        /// <summary>Children of the container holding <paramref name="children" />, each made by <paramref name="make" /> the first time its key is shown.</summary>
        public Keyed(Element.Collection children, Func<TKey, TElement> make)
        {
            _children = children;
            _make = make;
        }

        /// <summary>How many there are.</summary>
        public int Count => _kept.Count;

        /// <summary>The element for <paramref name="key" />, or null where it is not shown.</summary>
        public TElement this[TKey key]
        {
            get
            {
                Entry entry;
                return _kept.TryGetValue(key, out entry) ? entry.Element : null;
            }
        }

        /// <summary>Makes the children the elements for <paramref name="keys" />, in that order.</summary>
        /// <remarks>One pass and one lookup a key. A key given twice is shown once, where it first stands.</remarks>
        public void Show(IList<TKey> keys)
        {
            _showing++;
            var at = 0;

            for (var i = 0; i < keys.Count; i++)
            {
                Entry entry;
                if (!_kept.TryGetValue(keys[i], out entry))
                {
                    entry = new Entry { Element = _make(keys[i]) };
                    _kept.Add(keys[i], entry);
                }
                else if (entry.Showing == _showing)
                    continue;

                entry.Showing = _showing;

                if (at >= _children.Count || !ReferenceEquals(_children[at], entry.Element))
                    _children.Insert(at, entry.Element);

                at++;
            }

            // Every kept element now stands in front, in order, so whatever is behind them was not shown.
            while (_children.Count > at)
                _children.Remove(_children[_children.Count - 1]);

            if (_kept.Count == at)
                return;

            _gone.Clear();
            foreach (var pair in _kept)
            {
                if (pair.Value.Showing != _showing)
                    _gone.Add(pair.Key);
            }

            foreach (var key in _gone)
                _kept.Remove(key);
        }

        sealed class Entry
        {
            public TElement Element;
            public int Showing;
        }
    }
}
