using System;
using System.Collections.Generic;
using Lumel.Input;
using Lumel.Layout;

namespace Lumel.Controls
{
    /// <summary>Pages in one place with a strip of headers above them, the page of the header pressed shown.</summary>
    /// <remarks>The pages stand in a <see cref="Deck" />, so the control is as large as its largest page whichever shows.</remarks>
    public class Tabs : VStack
    {
        // Equal plates butted edge to edge, as the game draws the tabs across the top of its terminal.
        readonly Grid _strip = new Grid();
        readonly Deck _pages = new Deck();
        readonly List<Button> _headers = new List<Button>();

        ButtonStyle _headerStyle;
        ButtonStyle _currentStyle;
        int _current;

        /// <summary>An empty set of tabs.</summary>
        public Tabs()
        {
            Spacing = 12f;
            Children.Add(_strip);
            Children.Add(_pages);
        }

        /// <summary>How a header looks while its page is not shown.</summary>
        public ButtonStyle HeaderStyle
        {
            get { return _headerStyle; }
            set
            {
                _headerStyle = value;
                Show();
            }
        }

        /// <summary>How the header of the page shown looks.</summary>
        public ButtonStyle CurrentStyle
        {
            get { return _currentStyle; }
            set
            {
                _currentStyle = value;
                Show();
            }
        }

        /// <summary>Which page is shown, counted from nought.</summary>
        public int Current
        {
            get { return _current; }
            set
            {
                if (value < 0 || value >= _headers.Count)
                    return;

                _current = value;
                Show();
                if (Switched != null)
                    Switched(value);
            }
        }

        /// <summary>Raised when another page is shown, with which.</summary>
        public event Action<int> Switched;

        /// <summary>Adds a page under a header saying <paramref name="title" />, with <paramref name="about" /> under the pointer, and answers the header.</summary>
        public Button Add(string title, Element page, Tip about = null)
        {
            var index = _headers.Count;
            var header = new Button { Text = title, Tip = about, Command = new Relay(delegate { Current = index; }) };
            _headers.Add(header);
            _strip.Columns.Add(Length.Flex(1));
            _strip.Cells[0, index] = header;
            _pages.Children.Add(page);
            Show();
            return header;
        }

        void Show()
        {
            for (var i = 0; i < _headers.Count; i++)
                _headers[i].Style = i == _current ? _currentStyle : _headerStyle;

            if (_current < _pages.Children.Count)
                _pages.Top = _pages.Children[_current];
        }
    }
}
