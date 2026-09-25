using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>
    ///     The game's own on/off control: two halves side by side, both words always readable, the half that
    ///     is in force filled light and the other left dark.
    /// </summary>
    /// <remarks>Transcribed from the game's <c>MyGuiControlOnOffSwitch</c>.</remarks>
    public class Switch : Element
    {
        readonly Label _off;
        readonly Label _on;

        float _half;

        /// <remarks><paramref name="field" /> stands it among the boxes a value is given in, as tall as one.</remarks>
        public Switch(float fontSize, bool field = false)
        {
            var children = new ElementCollection(this);

            _on = Half(Widgets.Caps("on"), fontSize, field);
            _off = Half(Widgets.Caps("off"), fontSize, field);

            children.Add(_on);
            children.Add(_off);

            Edge = Palette.Dim;
            EdgeThickness = Space.Hair;

            HighlightEdge = Palette.Muted;
        }

        /// <summary>Which way it is set.</summary>
        public bool IsOn { get; private set; }

        /// <summary>Whether either half is in force, which it is not until somebody chooses.</summary>
        public bool IsSet { get; private set; }

        /// <summary>Renames the two halves.</summary>
        public void Says(string on, string off)
        {
            _on.Text = Widgets.Caps(on);
            _off.Text = Widgets.Caps(off);
        }

        /// <summary>Lights this where the pointer is resting on it.</summary>
        /// <remarks>Only for a switch that takes its own presses, as on a form; elsewhere the screen lights it.</remarks>
        public void Lit(Vector2 point)
        {
            IsHot = !Collapsed && Holds(point);
        }

        /// <summary>Shows neither half in force, for a question nobody has answered.</summary>
        public void Unchosen()
        {
            Collapsed = false;
            IsSet = false;
            IsOn = false;

            _on.Color = Palette.Faint;
            _off.Color = Palette.Faint;
            Edge = Palette.Dim;
        }

        /// <summary>Which half a point is on, and whether it is on the control at all.</summary>
        public bool Chose(Vector2 point, out bool isOn)
        {
            isOn = false;
            if (Collapsed || !Holds(point))
                return false;

            isOn = point.X < Bounds.X + Bounds.Width * 0.5f;
            return true;
        }

        /// <summary>Sets both halves at once, so the two can never disagree about which is lit.</summary>
        /// <param name="isOn">Which half is in force.</param>
        /// <param name="governs">Whether it switches more than one thing, which draws its edge brighter.</param>
        public void Show(bool isOn, bool governs)
        {
            Collapsed = false;
            IsSet = true;
            IsOn = isOn;

            // Further apart than the game's words, whose inactive font is (220, 244, 252) against white:
            // its switch is five times this size and lets the fills carry the state. The word out of
            // force stays at Faint, the floor of the text ramp; the unlit colour would read at 2.2:1
            // against the dark half.
            _on.Color = isOn ? Palette.Ink : Palette.Faint;
            _off.Color = isOn ? Palette.Faint : Palette.Ink;

            // Two steps of the text ramp, because the line and unlit tones are within a percent of each other.
            Edge = governs ? Palette.Faint : Palette.Dim;
        }

        /// <summary>Takes the switch out of the layout, where there is nothing to switch.</summary>
        public void Hide()
        {
            Collapsed = true;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _on.Measure(g, available);
            _off.Measure(g, available);

            // Both halves take the longer word's width, so the switch keeps its shape when switched.
            _half = _on.DesiredSize.X > _off.DesiredSize.X ? _on.DesiredSize.X : _off.DesiredSize.X;

            var height = _on.DesiredSize.Y > _off.DesiredSize.Y ? _on.DesiredSize.Y : _off.DesiredSize.Y;
            return new Vector2(_half * 2f, height);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _on.Arrange(new RectangleF(bounds.X, bounds.Y, _half, bounds.Height));
            _off.Arrange(new RectangleF(bounds.X + _half, bounds.Y, _half, bounds.Height));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            // Inside the border, so the fills do not cover it.
            var inset = EdgeThickness;
            var inner = new RectangleF(
                Bounds.X + inset, Bounds.Y + inset,
                Bounds.Width - inset * 2f, Bounds.Height - inset * 2f);

            // The dark tone under the whole, then the lit half on top, rather than two halves side by
            // side: each rectangle is rounded on its own, so two halves on a fractional width overlap or
            // leave a gap.
            g.DrawSprite(Solid, inner, Palette.SwitchDark, 0f);

            if (!IsSet)
            {
                _on.Draw(g);
                _off.Draw(g);
                return;
            }

            var half = inner.Width * 0.5f;
            g.DrawSprite(Solid, IsOn
                    ? new RectangleF(inner.X, inner.Y, half, inner.Height)
                    : new RectangleF(inner.X + half, inner.Y, inner.Width - half, inner.Height),
                Palette.SwitchLit, 0f);

            _on.Draw(g);
            _off.Draw(g);
        }

        static Label Half(string text, float fontSize, bool field)
        {
            var half = Widgets.Label(text, Palette.Faint, fontSize);
            half.Padding = new Thickness(Space.ChipX, field ? Space.FieldY : Space.ChipY);
            half.Centres = true;
            half.Wraps = false;
            return half;
        }
    }
}