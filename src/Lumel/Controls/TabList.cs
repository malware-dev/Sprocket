using System;
using System.Collections;
using System.Collections.Generic;

namespace Lumel.Controls
{
    /// <summary>The tabs of one <see cref="TabView" />, which the view's initializer fills.</summary>
    public sealed class TabList : IEnumerable<Tab>
    {
        readonly Action<Tab> _added;
        readonly List<Tab> _tabs = new List<Tab>();

        /// <summary>Tabs that tell <paramref name="added" /> of each one put after the others.</summary>
        public TabList(Action<Tab> added)
        {
            _added = added;
        }

        /// <summary>How many tabs there are.</summary>
        public int Count => _tabs.Count;

        /// <summary>The tab at <paramref name="index" />, counted from nought.</summary>
        public Tab this[int index] => _tabs[index];

        /// <summary>Puts <paramref name="tab" /> after the others.</summary>
        public void Add(Tab tab)
        {
            _tabs.Add(tab);
            _added(tab);
        }

        /// <inheritdoc />
        public IEnumerator<Tab> GetEnumerator()
        {
            return _tabs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
