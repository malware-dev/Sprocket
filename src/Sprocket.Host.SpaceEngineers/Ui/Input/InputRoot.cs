using System.Collections.Generic;
using VRage.Input;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Input
{
    /// <summary>Where the pointer and the keyboard enter one tree of elements: what holds the keyboard, and what holds the pointer.</summary>
    /// <remarks>The host turns the game's input into these calls; no element reads the game's input itself.</remarks>
    public sealed class InputRoot
    {
        readonly Element _root;

        /// <summary>Reused for walking the tree in order, so moving the focus allocates nothing once it has grown.</summary>
        readonly List<Element> _walk = new List<Element>();

        readonly List<Element> _order = new List<Element>();

        Element _captured;
        Element _focused;

        /// <summary>Input for the tree under <paramref name="root" />.</summary>
        public InputRoot(Element root)
        {
            _root = root;
            Clipboard = string.Empty;
        }

        /// <summary>What holds the keyboard, or null for nothing.</summary>
        public Element Focused => _focused;

        /// <summary>The text last copied or cut from anything under this root.</summary>
        public string Clipboard { get; set; }

        /// <summary>Gives the keyboard to <paramref name="element" />, or takes it from everything for null.</summary>
        public void Focus(Element element)
        {
            if (_focused == element)
                return;

            if (_focused != null)
                _focused.IsFocused = false;

            _focused = element;

            if (_focused != null)
                _focused.IsFocused = true;
        }

        /// <summary>A press beginning at <paramref name="point" />, the <paramref name="presses" />th in quick succession; answers whether anything took it.</summary>
        /// <remarks>Focus goes to the nearest focusable element at or above what was pressed, and leaves when nothing focusable was.</remarks>
        public bool PointerDown(Vector2 point, int presses)
        {
            var target = _root.Under(point);
            Focus(FocusableFrom(target));

            _captured = target;
            if (target == null)
                return false;

            target.OnPointerDown(point, presses);
            return true;
        }

        /// <summary>The pointer moving to <paramref name="point" />, which reaches whatever a press began on.</summary>
        public void PointerMove(Vector2 point)
        {
            if (_captured != null)
                _captured.OnPointerMove(point);
        }

        /// <summary>The press ending at <paramref name="point" />, carrying out the pressed element's command when it ends on it.</summary>
        public void PointerUp(Vector2 point)
        {
            var released = _captured;
            _captured = null;

            if (released == null)
                return;

            released.OnPointerUp(point);

            if (released.Command != null && released.IsEnabled && released.Holds(point))
                released.Command.Execute();
        }

        /// <summary>A key going down; answers whether anything took it.</summary>
        /// <remarks>What holds the keyboard is offered it first. Tab then moves the focus on, or back with shift, and Escape lets it go.</remarks>
        public bool Key(MyKeys key, bool shift, bool ctrl)
        {
            if (_focused != null && _focused.OnKey(this, key, shift, ctrl))
                return true;

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
            if (element.Collapsed)
                return;

            if (element.Focusable && element.IsEnabled)
                _order.Add(element);

            var from = _walk.Count;
            element.AddChildren(_walk);
            var to = _walk.Count;

            for (var i = from; i < to; i++)
                Collect(_walk[i]);

            _walk.RemoveRange(from, to - from);
        }

        static Element FocusableFrom(Element element)
        {
            for (var at = element; at != null; at = at.Parent)
            {
                if (at.Focusable && at.IsEnabled)
                    return at;
            }

            return null;
        }
    }
}
