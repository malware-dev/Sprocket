using System;
using System.Collections.Generic;
using Lumel.Input;
using Lumel.Layout;

namespace Lumel.Controls
{
    /// <summary>Pages in one place, with a strip of their headers above them, the page of the header pressed shown.</summary>
    /// <remarks>
    /// Only the page shown is in the view, so it is the only one laid out and drawn, and the view is as tall as that page.
    /// A page not shown keeps everything it holds, and comes back as it was.
    /// </remarks>
    public class TabView : VStack
    {
        // Equal plates butted edge to edge, as the game draws the tabs across the top of its terminal.
        readonly Grid _strip = new Grid();
        readonly TabList _tabs;
        readonly List<Relay> _presses = new List<Relay>();
        TabStyle _headerStyle;
        Element _shown;
        int _current;

        /// <summary>A view with no tabs yet.</summary>
        public TabView()
        {
            _tabs = new TabList(Added);
            Spacing = 12f;
            Children.Add(_strip);
        }

        /// <summary>The tabs, in the order their headers stand.</summary>
        public TabList Tabs => _tabs;

        /// <summary>How every header looks.</summary>
        public TabStyle HeaderStyle
        {
            get { return _headerStyle; }
            set
            {
                _headerStyle = value;
                foreach (var tab in _tabs)
                    tab.Style = value;
            }
        }

        /// <summary>Which tab's page is shown, counted from nought.</summary>
        public int Current
        {
            get { return _current; }
            set
            {
                if (value < 0 || value >= _tabs.Count || value == _current && _shown != null)
                    return;

                _current = value;
                Show();
                if (Switched != null)
                    Switched(value);
            }
        }

        /// <summary>Raised when another page is shown, with which.</summary>
        public event Action<int> Switched;

        /// <summary>Puts <paramref name="tab" /> after the others.</summary>
        void Added(Tab tab)
        {
            var index = _tabs.Count - 1;
            tab.Style = _headerStyle;
            _presses.Add(new Relay(() => Current = index));
            _strip.Columns.Add(GridLength.Flex(1));
            _strip.Cells[0, index] = tab;
            Show();
        }

        /// <summary>Marks the current tab, whose header has nothing to do when pressed, and puts its page in the view in place of the one there.</summary>
        void Show()
        {
            for (var i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].IsCurrent = i == _current;
                _tabs[i].Command = i == _current ? null : _presses[i];
            }

            var page = _current < _tabs.Count ? _tabs[_current].Content : null;
            if (ReferenceEquals(page, _shown))
                return;

            if (_shown != null)
                Children.Remove(_shown);

            _shown = page;
            if (page == null)
                return;

            Children.Add(page);
            page.Restyle();
        }
    }
}
