using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A logic system's list of things of one kind, <typeparamref name="T" />, and its empty categories, as a screen arranges them.</summary>
    /// <remarks>Made when something is arranged, never while a screen draws.</remarks>
    public sealed class Listed<T> : ICategorizedList where T : class, ICategorized
    {
        readonly List<T> _items;
        readonly List<EmptyCategory> _empty;

        /// <summary>The list <paramref name="items" />, with the categories with nothing under them in <paramref name="empty" />.</summary>
        public Listed(List<T> items, List<EmptyCategory> empty)
        {
            _items = items;
            _empty = empty;
        }

        /// <inheritdoc />
        public IReadOnlyList<ICategorized> Items => _items;

        /// <inheritdoc />
        public IList<EmptyCategory> Empty => _empty;

        /// <inheritdoc />
        public void Arrange(IList<ICategorized> order)
        {
            _items.Clear();
            for (var i = 0; i < order.Count; i++)
                _items.Add((T)order[i]);
        }
    }
}
