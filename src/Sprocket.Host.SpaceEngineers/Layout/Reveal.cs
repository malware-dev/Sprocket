using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;

namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>
    /// Shows part of one child, from the bottom up, and cuts off the rest.
    /// </summary>
    /// <remarks>
    /// What makes something slide in rather than appear. A banner that is simply added to a column takes
    /// its full height on the frame it arrives, and everything below it jumps down by that much - which
    /// is the one thing this screen is not allowed to do, because a list that moves under a pointer is a
    /// list somebody presses the wrong row in.
    /// <para>
    /// The child is always arranged at the height it asked for and is moved up out of view instead, so
    /// nothing inside it is ever squashed: text keeps its line breaks and a row keeps its height while
    /// the whole thing is part way in. <see cref="Element.ClipsContent" /> is what makes the part that is
    /// out of view invisible, and it is on from the start rather than something a caller sets.
    /// </para>
    /// </remarks>
    public class Reveal : Element
    {
        Element _child;
        float _openness;

        public Reveal()
        {
            // Not optional: without it the child draws in full while the layout is told it is shorter,
            // which looks exactly like the jump this exists to stop, plus overdraw.
            ClipsContent = true;
        }

        /// <summary>The one thing in here.</summary>
        public Element Child
        {
            get { return _child; }
            set
            {
                if (_child == value)
                    return;

                if (_child != null)
                    _child.Parent = null;

                _child = value;

                if (_child != null)
                    _child.Parent = this;

                InvalidateMeasure();
            }
        }

        /// <summary>
        /// How much of the child is in view, from none of it to all of it.
        /// </summary>
        /// <remarks>
        /// Drives the height this asks for, so setting it invalidates the measure: whatever is animating
        /// it is saying the layout has changed, and on the frames it is not being touched nothing here
        /// costs anything.
        /// </remarks>
        public float Openness
        {
            get { return _openness; }
            set
            {
                var wanted = value < 0f ? 0f : (value > 1f ? 1f : value);
                if (_openness == wanted)
                    return;

                _openness = wanted;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether none of the child is in view, which is when it may be taken away.</summary>
        public bool Shut => _openness <= 0f;

        /// <summary>
        /// Nothing inside this can be reached until all of it is in view.
        /// </summary>
        /// <remarks>
        /// A control that answers before it has arrived is a control somebody hits by accident on the
        /// way to whatever was underneath it, and the banner this exists for slides in over a list
        /// somebody is already pressing. Said here rather than in the walk, so that the walk and
        /// <see cref="Element.Reaches" /> - a press and a grab - cannot come to disagree about it.
        /// </remarks>
        internal override bool Passes
        {
            get { return _openness >= 1f; }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_child == null || _child.Collapsed)
                return Vector2.Zero;

            _child.Measure(g, available);

            return new Vector2(_child.DesiredSize.X, _child.DesiredSize.Y * _openness);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_child == null || _child.Collapsed)
                return;

            // At its full height, with the hidden part above the top edge. The bottom of the child sits
            // on the bottom of what is on show, so the words already in view stay exactly where they
            // are as more of them arrive.
            var height = _child.DesiredSize.Y;

            _child.Arrange(new RectangleF(bounds.X, bounds.Y + bounds.Height - height,
                bounds.Width, height));
        }

        protected override Element UnderContent(Vector2 point)
        {
            return Passes && _child != null ? _child.Under(point) : null;
        }

        protected override void DrawContent(IGraphics g)
        {
            // Nothing at all while it is shut. The clip would hide it either way, but the calls
            // would still be made and paid for on every frame of something nobody can see.
            if (Shut || _child == null || _child.Collapsed)
                return;

            _child.Draw(g);
        }
    }
}
