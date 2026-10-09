using VRageMath;
using Lumel.Styling;

namespace Lumel.Viewing
{
    /// <summary>What a scroll view looks like in one state, on top of what any element does.</summary>
    public class ScrollViewState : ElementState
    {
        /// <summary>How wide the bar is when a style leaves it to the neutral values.</summary>
        public const float NeutralBarWidth = 12f;

        /// <summary>How short the thumb may get when a style leaves it to the neutral values.</summary>
        public const float NeutralLeastThumb = 32f;

        /// <summary>The thumb's color when a style leaves it to the neutral values: a gray-blue that shows on a dark page and on a light one.</summary>
        public static readonly Color NeutralThumb = new Color(95, 114, 120);

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
                scroll.BarWidth = NeutralBarWidth;

            if (Thumb.HasValue)
                scroll.Thumb = NeutralThumb;

            if (LeastThumb.HasValue)
                scroll.LeastThumb = NeutralLeastThumb;
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
