using Lumel;
using Lumel.Controls;
using Lumel.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    // A .debug.cs file: compiled into local builds only and not packed for the game, where the call to Wrote
    // compiles to nothing.
    public sealed partial class EditorView
    {
        int _wires;

        /// <summary>How many times the chrome has written what the screen says onto its controls.</summary>
        public int Wires => _wires;

        /// <summary>Where a press reaches the first thing on the screen named or saying <paramref name="text" />, with or without an adder's plus.</summary>
        /// <remarks>Words hidden under a window or scrolled out of sight are passed over.</remarks>
        public bool Find(string text, out Vector2 at)
        {
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                if (Find(_windows[i], text, out at))
                    return true;
            }

            return Find(_root, text, out at);
        }

        partial void Wrote()
        {
            _wires++;
        }

        bool Find(Element element, string text, out Vector2 at)
        {
            at = default(Vector2);
            if (!element.IsVisible)
                return false;

            var button = element as Button;
            var label = element as Label;
            var glyph = element as Lumel.Images.Glyph;
            var said = element.AutomationId ?? (button != null ? button.Text : label != null ? label.Text : glyph?.Symbol);
            if (said == text || said == StatusRow.Plus + text)
            {
                var bounds = element.Bounds;
                var middle = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
                if (Reaches(Under(middle), element))
                {
                    at = middle;
                    return true;
                }
            }

            for (var i = 0; i < element.VisualChildCount; i++)
            {
                if (Find(element.GetVisualChild(i), text, out at))
                    return true;
            }

            return false;
        }

        /// <summary>Whether a press landing on <paramref name="hit" /> is a press on <paramref name="element" />: it, or something it is part of.</summary>
        static bool Reaches(Element hit, Element element)
        {
            for (var at = element; at != null; at = at.Parent)
            {
                if (at == hit)
                    return true;
            }

            return false;
        }
    }
}
