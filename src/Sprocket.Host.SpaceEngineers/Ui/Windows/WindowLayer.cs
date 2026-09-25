using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
{
    /// <summary>The windows open over a screen, in the order they were opened: drawn after it, last on top, and asked about the pointer before it, top first.</summary>
    public sealed class WindowLayer
    {
        readonly List<Window> _open = new List<Window>();

        /// <summary>How many windows are open.</summary>
        public int Count => _open.Count;

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

        /// <summary>Puts <paramref name="window" /> on top; one already open is left where it is.</summary>
        public void Open(Window window)
        {
            if (!_open.Contains(window))
                _open.Add(window);
        }

        /// <summary>Takes <paramref name="window" /> away, if it is open.</summary>
        public void Close(Window window)
        {
            _open.Remove(window);
        }

        /// <summary>Whether <paramref name="window" /> is open.</summary>
        public bool IsOpen(Window window)
        {
            return _open.Contains(window);
        }

        /// <summary>Lays out and draws every open window over <paramref name="area" />, each modal one over a backdrop covering everything beneath it.</summary>
        /// <remarks>A window is as wide as its placement says where it says, and otherwise as wide as it asks, up to the area.</remarks>
        public void Draw(IGraphics g, RectangleF area)
        {
            for (var i = 0; i < _open.Count; i++)
            {
                var window = _open[i];

                if (window.IsModal && window.Backdrop.A > 0)
                    g.Fill(area, window.Backdrop);

                var sized = window.Placement as ISized;
                var held = sized != null && !float.IsNaN(sized.Width);

                window.Measure(g, new Vector2(held ? sized.Width : area.Width, area.Height));

                var size = window.DesiredSize;
                if (held)
                    size.X = sized.Width;

                window.Arrange(window.Place(area, size));
                window.Draw(g);
            }
        }

        /// <summary>What a press at <paramref name="point" /> reaches in the windows, or null; <paramref name="stopped" /> says whether a modal window keeps it from the screen beneath.</summary>
        public Element Under(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var hit = _open[i].Under(point);
                if (hit != null)
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

        /// <summary>What the pointer at <paramref name="point" /> rests on in the windows that has something to say, or null; <paramref name="stopped" /> as for <see cref="Under" />.</summary>
        public Element SayingAt(Vector2 point, out bool stopped)
        {
            for (var i = _open.Count - 1; i >= 0; i--)
            {
                var found = _open[i].SayingAt(point);
                if (found != null || _open[i].IsModal)
                {
                    stopped = true;
                    return found;
                }
            }

            stopped = false;
            return null;
        }
    }
}
