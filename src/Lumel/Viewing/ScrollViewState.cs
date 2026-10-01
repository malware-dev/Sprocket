using VRageMath;
using Lumel.Styling;

namespace Lumel.Viewing
{
    /// <summary>What a scroll view looks like in one state, on top of what any element does.</summary>
    public class ScrollViewState : ElementState
    {
        /// <summary>How wide the bar is, or null to leave it to the layers beneath.</summary>
        public float? BarWidth { get; set; }

        /// <summary>The thumb's color, or null to leave it to the layers beneath.</summary>
        public Color? Thumb { get; set; }

        /// <summary>How short the thumb may get, or null to leave it to the layers beneath.</summary>
        public float? LeastThumb { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var scroll = element as ScrollView;
            if (scroll == null)
                return;

            if (BarWidth.HasValue)
                scroll.BarWidth = 0f;

            if (Thumb.HasValue)
                scroll.Thumb = Color.Transparent;

            if (LeastThumb.HasValue)
                scroll.LeastThumb = 0f;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var scroll = element as ScrollView;
            if (scroll == null)
                return;

            if (BarWidth.HasValue)
                scroll.BarWidth = BarWidth.Value;

            if (Thumb.HasValue)
                scroll.Thumb = Thumb.Value;

            if (LeastThumb.HasValue)
                scroll.LeastThumb = LeastThumb.Value;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            BarWidth = null;
            Thumb = null;
            LeastThumb = null;
        }

        /// <inheritdoc />
        public override void Gather(ElementState layer)
        {
            base.Gather(layer);
            var other = layer as ScrollViewState;
            if (other == null)
                return;

            if (!BarWidth.HasValue)
                BarWidth = other.BarWidth;

            if (!Thumb.HasValue)
                Thumb = other.Thumb;

            if (!LeastThumb.HasValue)
                LeastThumb = other.LeastThumb;
        }
    }
}
