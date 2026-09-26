using Sprocket.Host.SpaceEngineers.Ui.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
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

            scroll.BarWidth = 0f;
            scroll.Thumb = Color.Transparent;
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
    }
}
