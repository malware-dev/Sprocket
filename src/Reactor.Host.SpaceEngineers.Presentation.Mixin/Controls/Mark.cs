using VRage.Game;
using VRage.Utils;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Host.SpaceEngineers.Presentation.Controls
{
    /// <summary>
    /// The box or circle at the head of a row somebody is choosing from.
    /// </summary>
    /// <remarks>
    /// Square where several may be chosen and round where only one may, which is the one piece of this
    /// screen borrowed from desktops rather than from the game. Space Engineers has a checkbox and no
    /// radio button, because nothing in it asks a question that way; the shape still carries the answer
    /// to "may I pick more than one of these", and that is worth more here than the consistency is.
    /// <para>
    /// The square is drawn and the circle is a sprite, for the same reason everything else here is one or
    /// the other: a rectangle is exact at any size and a curve cannot be made of them.
    /// </para>
    /// </remarks>
    public class Mark : Element
    {
        readonly Glyph _glyph = Widgets.Symbol(Symbols.Ring, Palette.Faint);

        public Mark()
        {
            Color = Palette.Faint;
        }

        /// <summary>Whether more than one of the rows this heads may be chosen.</summary>
        private bool _many;
        private bool _chosen;
        private Color _color;

        /// <summary>
        /// Whether this is one of several that can be held at once.
        /// </summary>
        /// <remarks>
        /// Asks for a new measure when it changes, as do the two below it. Which glyph this is gets
        /// decided in <see cref="MeasureContent" /> - it has to be, because a glyph centres itself
        /// against the line it was measured with - so a change that does not ask for a measure is a
        /// change that never reaches the screen.
        /// </remarks>
        public bool Many
        {
            get { return _many; }
            set
            {
                if (_many == value)
                    return;

                _many = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether this one has been.</summary>
        public bool Chosen
        {
            get { return _chosen; }
            set
            {
                if (_chosen == value)
                    return;

                _chosen = value;
                InvalidateMeasure();
            }
        }

        public Color Color
        {
            get { return _color; }
            set
            {
                if (_color.Equals(value))
                    return;

                _color = value;
                InvalidateMeasure();
            }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            // The circle is measured here even on a row that will draw a square, because which one this
            // is can change between a measure and a draw - a stage that allowed one choice and now allows
            // several - and a glyph arranged without having been measured centres itself against nothing.
            // That was worth an hour: the ring drew low and to the right of where it belonged, which
            // reads as bad artwork rather than as a missed call.
            _glyph.Symbol = Many ? Symbols.Check : (Chosen ? Symbols.Disc : Symbols.Ring);
            _glyph.Scale = Many ? Lettering.Symbol * 0.58f : Lettering.Symbol;
            _glyph.Tint = Color;
            _glyph.Measure(g, available);

            return new Vector2(Sizes.Mark, Sizes.Mark);
        }

        protected override void DrawContent(IGraphics g)
        {
            if (!Many)
            {
                // Given the whole box rather than a square cut out of it, because a glyph centres its own
                // line inside whatever it is handed and that line is taller than the square a drawn
                // checkbox wants.
                _glyph.Arrange(ContentBounds);
                _glyph.Draw(g);
                return;
            }

            RectangleF box = new RectangleF(
                ContentBounds.X + (ContentBounds.Width - Sizes.Mark) * 0.5f,
                ContentBounds.Y + (ContentBounds.Height - Sizes.Mark) * 0.5f,
                Sizes.Mark, Sizes.Mark);

            Outline(g, box, Color, Space.Hair);

            // A tick, and not the smaller filled square the game itself draws. That square is what every
            // other interface uses for "partly", and this audience learned checkboxes everywhere except
            // here.
            //
            // Sized down from the ring's scale, which is the one difference between them: the ring is
            // drawn *instead of* a box and may fill the space, while the tick is drawn *inside* one and at
            // full size climbs out of it. Two thirds still caught the top-right corner; this clears it.
            if (Chosen)
            {
                _glyph.Arrange(ContentBounds);
                _glyph.Draw(g);
            }
        }
    }
}
