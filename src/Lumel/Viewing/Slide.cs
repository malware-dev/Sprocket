using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;

namespace Lumel.Viewing
{
    /// <summary>One content sliding in from the right as another slides out to the left, side by side.</summary>
    /// <remarks>
    /// Both are laid out in the whole of the slide, and how far it has gone moves them only as they are drawn, so a slide in
    /// motion is never laid out again. While it moves each is drawn as far outside the slide as <see cref="Travel" /> takes
    /// it, which suits a slide whose sides reach the screen's, or one inside something that clips.
    /// </remarks>
    public class Slide : Layers
    {
        Element _leaving;
        Element _coming;
        float _progress = 1f;
        float _travel;

        /// <summary>The content sliding out, or null for none.</summary>
        public Element Leaving
        {
            get { return _leaving; }
            set { Swap(ref _leaving, value); }
        }

        /// <summary>The content sliding in, which is the one in place once the slide is done, or null for none.</summary>
        public Element Coming
        {
            get { return _coming; }
            set { Swap(ref _coming, value); }
        }

        /// <summary>How far the slide has gone, from 0, where the leaving content is in place, to 1, where the coming one is.</summary>
        public float Progress
        {
            get { return _progress; }
            set
            {
                _progress = value < 0f ? 0f : value > 1f ? 1f : value;
                Move();
            }
        }

        /// <summary>How far apart the two contents are drawn, or 0 for the slide's own width.</summary>
        public float Travel
        {
            get { return _travel; }
            set
            {
                _travel = value;
                Move();
            }
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);
            Move();
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (_leaving != null && _progress < 1f)
                _leaving.Draw(g);

            _coming?.Draw(g);
        }

        /// <summary>Only the coming content is pressed, and only once it is in place.</summary>
        protected override Element UnderContent(Vector2 point)
        {
            return _progress >= 1f ? _coming?.Under(point) : null;
        }

        /// <summary>Moves each content as far across as the slide has gone, which is where it is drawn and pressed.</summary>
        void Move()
        {
            var apart = _travel > 0f ? _travel : Bounds.Width;
            if (_leaving != null)
                _leaving.RenderTransform = _progress <= 0f ? RenderTransform.Identity : RenderTransform.Moved(new Vector2(-apart * _progress, 0f));

            if (_coming != null)
                _coming.RenderTransform = _progress >= 1f ? RenderTransform.Identity : RenderTransform.Moved(new Vector2(apart * (1f - _progress), 0f));
        }

        void Swap(ref Element held, Element value)
        {
            if (ReferenceEquals(held, value))
                return;

            if (held != null)
            {
                held.RenderTransform = RenderTransform.Identity;
                Children.Remove(held);
            }

            held = value;
            if (value != null)
                Children.Add(value);

            Move();
        }
    }
}
