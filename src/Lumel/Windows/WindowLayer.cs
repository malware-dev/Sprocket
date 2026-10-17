using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Viewing;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>The windows open over a screen, in the order they were opened: drawn after it, last on top, and asked about the pointer before it, top first.</summary>
    public sealed class WindowLayer
    {
        readonly List<Window> _open = new List<Window>();

        /// <summary>How many windows are open.</summary>
        public int Count => _open.Count;

        /// <summary>The open window <paramref name="index" /> places from the bottom, the one opened first being nought.</summary>
        public Window this[int index] => _open[index];

        /// <summary>Whether every open window is a floating one, which leaves the screen beneath free to use around it.</summary>
        public bool Floats
        {
            get
            {
                foreach (var window in _open)
                {
                    if (!(window is FloatingWindow))
                        return false;
                }

                return true;
            }
        }

        /// <summary>The window opened last, which is drawn over the rest, or null where none is open.</summary>
        public Window Top => _open.Count > 0 ? _open[_open.Count - 1] : null;

        /// <summary>The topmost modal window, which takes all input, or null where none is open.</summary>
        public Window Modal
        {
            get
            {
                for (var i = _open.Count - 1; i >= 0; i--)
                {
                    if (_open[i].IsModal)
                        return _open[i];
                }

                return null;
            }
        }

        /// <summary>Makes this the layer over <paramref name="screen" />, so everything on it opens its windows here.</summary>
        public void Covers(Element screen)
        {
            ((ILayered)screen).Layer = this;
        }

        /// <summary>Puts <paramref name="window" /> on top; one already open is left where it is.</summary>
        public void Open(Window window)
        {
            if (_open.Contains(window))
                return;

            _open.Add(window);
            ((ILayered)window).Layer = this;
        }

        /// <summary>Takes <paramref name="window" /> away, if it is open.</summary>
        public void Close(Window window)
        {
            if (!_open.Remove(window))
                return;

            ((ILayered)window).Layer = null;
            ((IClosingWindow)window).TakenAway();
        }

        /// <summary>Whether <paramref name="window" /> is open.</summary>
        public bool IsOpen(Window window)
        {
            return _open.Contains(window);
        }

        /// <summary>Lays out and draws every open window over <paramref name="area" />, each modal one over a backdrop covering everything beneath it.</summary>
        /// <remarks>A window is as wide and as tall as its placement says where it says, and otherwise as it asks, up to the area.</remarks>
        public void Draw(IGraphics g, RectangleF area)
        {
            foreach (var window in _open)
            {
                if (window.IsModal && window.Backdrop.A > 0)
                    g.Fill(area, window.Backdrop);

                var sized = window.Placement as ISized;
                var wide = sized != null && !float.IsNaN(sized.Width);
                var tall = sized != null && !float.IsNaN(sized.Height);

                var floating = window as FloatingWindow;
                if (floating != null)
                    floating.Screen = g.ScreenBounds;

                var room = window.Room(area);
                window.Measure(g, new Vector2(wide ? sized.Width : room.X, tall ? sized.Height : room.Y));

                var size = window.DesiredSize;
                if (tall)
                    size.Y = sized.Height;

                if (wide)
                    size.X = sized.Width;

                window.Arrange(window.Place(area, size));
                window.DrawBehind(g);
                window.Draw(g);
            }
        }

        /// <summary>Closes every popup a press at <paramref name="point" /> misses, from the top down to the first window it lands in, and answers whether any closed.</summary>
        /// <remarks>For whoever takes presses to ask before anything else, since a press that closes a popup goes no further.</remarks>
        public bool Dismiss(Vector2 point)
        {
            var closed = false;
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var window = _open[i];
                if (On(window, point) || !(window is Popup))
                    break;

                _open.RemoveAt(i);
                ((ILayered)window).Layer = null;
                ((IClosingWindow)window).TakenAway();
                closed = true;
            }

            return closed;
        }

        /// <summary>What a press at <paramref name="point" /> reaches in the windows, or null; <paramref name="stopped" /> says whether a window there, or a modal one, keeps it from the screen beneath.</summary>
        /// <remarks>A window keeps every press on it to itself, where something in it takes the press or not.</remarks>
        public Element Under(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var hit = _open[i].Under(point);
                if (hit != null || On(_open[i], point))
                {
                    stopped = true;
                    return hit;
                }

                if (_open[i].IsModal)
                {
                    stopped = true;
                    return null;
                }
            }

            stopped = false;
            return null;
        }

        /// <summary>What a wheel turned at <paramref name="point" /> moves in the windows, or null; <paramref name="stopped" /> says whether a window there, or a modal one, keeps it from the screen beneath.</summary>
        public IScrolls WheelAt(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var window = _open[i];
                if (On(window, point) || window.IsModal)
                {
                    stopped = true;
                    return window.WheelAt(point);
                }
            }

            stopped = false;
            return null;
        }

        /// <summary>What the pointer at <paramref name="point" /> rests on in the windows that has something to say, or null; <paramref name="stopped" /> as for <see cref="Under" />.</summary>
        public Element SayingAt(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var found = _open[i].SayingAt(point);
                if (found != null || On(_open[i], point) || _open[i].IsModal)
                {
                    stopped = true;
                    return found;
                }
            }

            stopped = false;
            return null;
        }

        /// <summary>Whether <paramref name="point" /> is on <paramref name="window" />: anywhere in it, whether or not what is there takes input.</summary>
        static bool On(Window window, Vector2 point)
        {
            return window.IsVisible && window.Bounds.Contains(point);
        }
    }
}
