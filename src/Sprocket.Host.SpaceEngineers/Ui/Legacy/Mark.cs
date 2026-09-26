using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>The box or circle at the head of a row somebody is choosing from.</summary>
    public class Mark : Box
    {
        readonly Glyph _glyph = Widgets.Centred(Symbols.Ring, Palette.Faint);

        bool _chosen;
        Color _color;

        bool _many;

        /// <summary>Makes a mark for a row that has not been chosen.</summary>
        public Mark()
        {
            Color = Palette.Faint;
        }

        /// <summary>Whether this is one of several that can be held at once.</summary>
        /// <remarks>
        ///     This, <see cref="Chosen" /> and <see cref="Color" /> all invalidate the measure, colour included,
        ///     because the glyph is picked and tinted in <see cref="MeasureContent" />: a glyph centres itself
        ///     against the line it was measured with.
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

        /// <summary>Whether this row has been chosen.</summary>
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

        /// <summary>The colour the mark is tinted.</summary>
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

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _glyph.Symbol = Many ? Symbols.Check : Chosen ? Symbols.Disc : Symbols.Ring;

            // The tick is drawn inside the box and at the ring's size climbs out of it. Sized by eye.
            _glyph.FontSize = Many ? Lettering.Symbol * 0.58f : Lettering.Symbol;

            // Only the ring stands beside text, so only the ring takes the lift a symbol beside text takes.
            _glyph.Lift = Many ? 0f : Lettering.SymbolLift;
            _glyph.Tint = Color;
            _glyph.Measure(g, available);

            return new Vector2(Sizes.Mark, Sizes.Mark);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (!Many)
            {
                // The whole box, not a square cut from it: a glyph centres its own line in what it is
                // handed, and that line is taller than the square.
                _glyph.Arrange(ContentBounds);
                _glyph.Draw(g);
                return;
            }

            var box = new RectangleF(
                ContentBounds.X + (ContentBounds.Width - Sizes.Mark) * 0.5f,
                ContentBounds.Y + (ContentBounds.Height - Sizes.Mark) * 0.5f,
                Sizes.Mark, Sizes.Mark);

            Outline(g, box, Color, Space.Hair);

            if (Chosen)
            {
                _glyph.Arrange(ContentBounds);
                _glyph.Draw(g);
            }
        }
    }
}