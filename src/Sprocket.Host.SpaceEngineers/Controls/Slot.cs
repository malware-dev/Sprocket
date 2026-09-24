using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Controls
{
    /// <summary>
    /// A fillable part of a sentence.
    /// </summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means, so nothing else may borrow this
    /// look. The line under it is what distinguishes it from a row at a glance, and it is drawn rather
    /// than being a border because the seam has no borders - a rectangle of colour is all either target
    /// can be asked for.
    /// </remarks>
    public class Slot : Label
    {
        /// <summary>The room held for the cross, where there is one.</summary>
        /// <remarks>
        /// A fixed number rather than the glyph's own width, so the words inside a slot do not shift
        /// sideways because a symbol font measured a hair wider than the last one did.
        /// </remarks>
        const float Clearing = 20f;

        readonly ElementCollection _children;

        Element _clear;

        public Slot()
        {
            _children = new ElementCollection(this);

            Scale = Lettering.Body;
            Color = Palette.Ink;
            Background = Palette.SlotFill;
            Highlight = Palette.ControlHot;
            Padding = new Thickness(Space.ChipX, Space.ChipY);
            Underline = Palette.SlotEdge;

            // A slot is one thing named, and a wrapped one would stop reading as a single chip.
            Wraps = false;
        }

        /// <summary>The colour of the line along the bottom.</summary>
        public Color Underline { get; set; }

        /// <summary>
        /// The way to empty this slot, drawn inside it at the right, or nothing.
        /// </summary>
        /// <remarks>
        /// Inside the chip rather than beside it, because what it empties is this slot. Outside, in
        /// the gap between two parts of a sentence, it reads as belonging to the whole line - and the
        /// line has its own mark at the end of the row, which destroys rather than empties.
        /// </remarks>
        public Element Clear
        {
            get { return _clear; }
            set
            {
                if (_clear == value)
                    return;

                if (_clear != null)
                    _children.Remove(_clear);

                if (value != null)
                    _children.Add(value);

                _clear = value;

                // The words are measured against the room left over, which this changes.
                Padding = new Thickness(Space.ChipX, Space.ChipY,
                    value == null ? Space.ChipX : Space.ChipX + Clearing, Space.ChipY);
            }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            Vector2 words = base.MeasureContent(g, available);
            if (_clear == null)
                return words;

            _clear.Measure(g, new Vector2(Clearing, words.Y));
            return words;
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);

            if (_clear == null)
                return;

            // In the room the padding held for it: against the right edge of the chip, centred on the
            // line the words sit on.
            Vector2 wanted = _clear.DesiredSize;
            float x = Bounds.X + Bounds.Width - Space.ChipX * 0.5f - wanted.X;
            float y = bounds.Y + (bounds.Height - wanted.Y) * 0.5f;

            _clear.Arrange(new RectangleF(x, y, wanted.X, wanted.Y));
        }

        protected override void DrawContent(IGraphics g)
        {
            const float thickness = 2f;

            g.DrawSprite(Solid,
                new RectangleF(Bounds.X, Bounds.Y + Bounds.Height - thickness, Bounds.Width, thickness),
                Underline, 0f);

            base.DrawContent(g);

            if (_clear != null)
                _clear.Draw(g);
        }

        /// <summary>The cross answers before the chip it sits in, so it can be pressed at all.</summary>
        protected override Element UnderContent(Vector2 point)
        {
            return _clear == null ? null : _clear.Under(point);
        }
    }
}
