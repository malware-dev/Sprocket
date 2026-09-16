using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Look;

namespace Reactor.Host.SpaceEngineers.Presentation.Controls
{
    /// <summary>
    /// The game's own on/off control: two halves side by side, both words always readable, the half that
    /// is in force filled light and the other left dark.
    /// </summary>
    /// <remarks>
    /// Copied from <c>MyGuiControlOnOffSwitch</c> rather than invented. That control is a bordered box
    /// holding two half-width checkboxes and two labels; the state is shown by which half is checked and
    /// by the font each label is drawn in - White on the side in force, Blue on the side that is not.
    /// Every part of it is a flat fill, a one-pixel border or a word, so all of it is reproducible here.
    /// <para>
    /// It says <b>what state the thing is in</b>, where everything else on these screens says what
    /// pressing it does. That is the game's convention and it is worth the inconsistency: an audience
    /// who have never programmed should not have to work out whether a lone word is a label or a button,
    /// and a switch showing both of its positions cannot be read the wrong way round.
    /// </para>
    /// </remarks>
    public class Switch : Element
    {
        readonly ElementCollection _children;
        readonly Label _on;
        readonly Label _off;

        float _half;

        public Switch(float scale)
        {
            _children = new ElementCollection(this);

            _on = Half(Widgets.Caps("on"), scale);
            _off = Half(Widgets.Caps("off"), scale);

            _children.Add(_on);
            _children.Add(_off);

            Edge = Palette.Dim;
            EdgeThickness = Space.Hair;

            // A switch is two fills and a border, so the border is the only response it has room for.
            // The game does the same: its highlight textures differ from its normal ones at the edge far
            // more than in the middle.
            HighlightEdge = Palette.Muted;
        }

        /// <summary>Which way it is set. The lit half and the bright word both follow this.</summary>
        public bool IsOn { get; private set; }

        /// <summary>
        /// Whether this governs everything below it as well as itself.
        /// </summary>
        /// <remarks>
        /// Two of them in a column need telling apart, because stopping a whole airlock is not the same
        /// as stopping one of its sequences. The indent already says which is nested; this says which is
        /// the bigger act, as a brighter border - a solid fill is spoken for, since both halves are
        /// drawn.
        /// </remarks>
        public bool Governs { get; private set; }

        /// <summary>Sets both halves at once, so the two can never disagree about which is lit.</summary>
        public void Show(bool isOn, bool governs)
        {
            Collapsed = false;
            IsOn = isOn;
            Governs = governs;

            // The game leaves both words nearly as bright as each other - its inactive font is
            // (220, 244, 252) against a white one - and lets the difference between the two fills carry
            // the state. That works at the size it draws a switch, which is a hundred and seventy pixels
            // to a half; ours is a fifth of that, so the word has to help.
            //
            // But only help. The unlit colour puts the off word at 2.2:1 against the dark half, which
            // makes a control whose whole promise is that both of its positions can be read into one that
            // cannot. The bottom of the text ramp is the floor here as everywhere: quiet, and still a
            // word.
            _on.Color = isOn ? Palette.Ink : Palette.Faint;
            _off.Color = isOn ? Palette.Faint : Palette.Ink;

            // Two steps of the text ramp rather than a line tone and an unlit one, which are within a
            // percent of each other and would turn the difference between stopping one sequence and
            // stopping the whole airlock into no difference at all.
            Edge = governs ? Palette.Faint : Palette.Dim;
        }

        /// <summary>Nothing to switch here, so it takes up no room at all.</summary>
        public void Hide()
        {
            Collapsed = true;
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _on.Measure(g, available);
            _off.Measure(g, available);

            // Both halves take the width of the longer word. Otherwise the control would change shape as
            // it was switched, and a row of them down a screen would not line up with each other.
            _half = _on.DesiredSize.X > _off.DesiredSize.X ? _on.DesiredSize.X : _off.DesiredSize.X;

            float height = _on.DesiredSize.Y > _off.DesiredSize.Y ? _on.DesiredSize.Y : _off.DesiredSize.Y;
            return new Vector2(_half * 2f, height);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            _on.Arrange(new RectangleF(bounds.X, bounds.Y, _half, bounds.Height));
            _off.Arrange(new RectangleF(bounds.X + _half, bounds.Y, _half, bounds.Height));
        }

        protected override void DrawContent(IGraphics g)
        {
            // Inside the border rather than over it, so the one pixel the game draws its controls with
            // survives the fills going on top of it.
            float inset = EdgeThickness;
            var inner = new RectangleF(
                Bounds.X + inset, Bounds.Y + inset,
                Bounds.Width - inset * 2f, Bounds.Height - inset * 2f);

            // The dark tone underneath the whole of it, then the lit half over the top. Two halves drawn
            // side by side is the obvious way and it is wrong: a backend rounds each rectangle on its own,
            // so on a fractional width the two either overlap by a pixel or leave a line of whatever is
            // behind them showing through the middle. One rectangle cannot have a seam in it.
            g.DrawSprite(Solid, inner, Palette.SwitchDark, 0f);

            float half = inner.Width * 0.5f;
            g.DrawSprite(Solid, IsOn
                ? new RectangleF(inner.X, inner.Y, half, inner.Height)
                : new RectangleF(inner.X + half, inner.Y, inner.Width - half, inner.Height),
                Palette.SwitchLit, 0f);

            _on.Draw(g);
            _off.Draw(g);
        }

        static Label Half(string text, float scale)
        {
            Label half = Widgets.Label(text, Palette.Faint, scale);
            half.Padding = new Thickness(Space.ChipX, Space.ChipY);
            half.Centres = true;
            half.Wraps = false;
            return half;
        }
    }
}
