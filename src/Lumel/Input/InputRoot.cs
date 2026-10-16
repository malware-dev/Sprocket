using System;
using System.Collections.Generic;
using VRage.Input;
using Lumel.Windows;
using VRageMath;

namespace Lumel.Input
{
    /// <summary>Where the pointer and the keyboard enter one tree of elements: what holds the keyboard, and what holds the pointer.</summary>
    /// <remarks>The host turns the game's input into these calls; no element reads the game's input itself.</remarks>
    public sealed class InputRoot
    {
        readonly Element _root;


        readonly List<Element> _order = new List<Element>();

        Element _captured;
        Element _focused;

        /// <summary>Input for the tree under <paramref name="root" />.</summary>
        public InputRoot(Element root)
        {
            _root = root;
            Clipboard = new Clipboard();
        }

        /// <summary>What holds the keyboard, or null for nothing.</summary>
        public Element Focused => _focused;

        /// <summary>Where text copied or cut from anything under this root is kept, and pasted from; in memory unless the host says otherwise.</summary>
        public IClipboard Clipboard { get; set; }

        /// <summary>Raised after the keyboard has moved to something else, or to nothing.</summary>
        public event Action FocusChanged;

        /// <summary>Gives the keyboard to <paramref name="element" />, or takes it from everything for null.</summary>
        public void Focus(Element element)
        {
            if (_focused == element)
                return;

            if (_focused != null)
                _focused.OnBlur();

            _focused = element;

            if (_focused != null)
                _focused.OnFocus();

            var changed = FocusChanged;
            if (changed != null)
                changed();
        }

        /// <summary>
        ///     A press beginning at <paramref name="point" />, the <paramref name="presses" />th in quick succession, with
        ///     <paramref name="shift" /> and <paramref name="ctrl" /> saying whether those keys were held; answers whether
        ///     anything took it.
        /// </summary>
        /// <remarks>Focus goes to the nearest focusable element at or above what was pressed, and leaves when nothing focusable was.</remarks>
        public bool PointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            return PointerDown(_root.Under(point), point, presses, shift, ctrl);
        }

        /// <summary>The same, on a <paramref name="target" /> the host has already found, as one that layers other trees over this one does.</summary>
        public bool PointerDown(Element target, Vector2 point, int presses, bool shift, bool ctrl)
        {
            Focus(FocusableFrom(target));

            _captured = target;
            if (target == null)
                return false;

            target.OnPointerDown(target.FromScreen(point), presses, shift, ctrl);
            return true;
        }

        /// <summary>The pointer moving to <paramref name="point" />, which reaches whatever a press began on.</summary>
        public void PointerMove(Vector2 point)
        {
            if (_captured != null)
                _captured.OnPointerMove(_captured.FromScreen(point));
        }

        /// <summary>The press ending at <paramref name="point" />, carrying out the pressed element's command when it ends on it.</summary>
        public void PointerUp(Vector2 point)
        {
            var released = _captured;
            _captured = null;

            if (released == null)
                return;

            var local = released.FromScreen(point);
            released.OnPointerUp(local);

            if (released.Command != null && released.IsEnabled && released.Holds(local))
                released.Command.Execute();
        }

        /// <summary>A key going down; answers whether anything took it.</summary>
        /// <remarks>
        /// What holds the keyboard is offered it first, and then whatever holds that, out to the root. Tab then moves the
        /// focus on, or back with shift, and Escape lets it go.
        /// </remarks>
        public bool Key(MyKeys key, bool shift, bool ctrl)
        {
            for (var at = _focused; at != null; at = Above(at))
            {
                if (at.OnKey(this, key, shift, ctrl))
                    return true;
            }

            if (key == MyKeys.Tab)
                return FocusNext(shift);

            if (key == MyKeys.Escape && _focused != null)
            {
                Focus(null);
                return true;
            }

            return false;
        }

        /// <summary>A character typed, which goes to what holds the keyboard.</summary>
        public void Text(char c)
        {
            if (_focused != null)
                _focused.OnText(c);
        }

        /// <summary>Moves the focus to the next focusable element in the order the tree is built in, or the one before; answers whether there was one.</summary>
        public bool FocusNext(bool back)
        {
            _order.Clear();
            Collect(_root);

            if (_order.Count == 0)
                return false;

            var at = _focused == null ? -1 : _order.IndexOf(_focused);
            int next;

            if (at < 0)
                next = back ? _order.Count - 1 : 0;
            else
                next = (at + (back ? _order.Count - 1 : 1)) % _order.Count;

            Focus(_order[next]);
            return true;
        }

        void Collect(Element element)
        {
            if (!element.IsVisible)
                return;

            if (element.Focusable && element.IsEnabled && (element.InputTransparency & InputTransparency.Self) == 0)
                _order.Add(element);

            if ((element.InputTransparency & InputTransparency.Children) != 0)
                return;

            for (var i = 0; i < element.VisualChildCount; i++)
                Collect(element.GetVisualChild(i));
        }

        /// <summary>What holds <paramref name="element" />: its parent, or for a popup, what it hangs from.</summary>
        static Element Above(Element element)
        {
            if (element.Parent != null)
                return element.Parent;

            var popup = element as Popup;
            return popup?.Anchor;
        }

        static Element FocusableFrom(Element element)
        {
            for (var at = element; at != null; at = Above(at))
            {
                if (at.Focusable && at.IsEnabled && (at.InputTransparency & InputTransparency.Self) == 0)
                    return at;
            }

            return null;
        }
    }
}
