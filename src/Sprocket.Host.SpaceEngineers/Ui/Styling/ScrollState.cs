using Sprocket.Host.SpaceEngineers.Ui.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a scroll looks like in one state, on top of what any element does.</summary>
    public class ScrollState : ElementState
    {
        float? _barWidth;
        Color? _thumb;
        float? _leastThumb;

        /// <summary>How wide the bar is, or null to leave it to the layers beneath.</summary>
        public float? BarWidth
        {
            get { return _barWidth; }
            set
            {
                Check();
                _barWidth = value;
            }
        }

        /// <summary>The thumb's color, or null to leave it to the layers beneath.</summary>
        public Color? Thumb
        {
            get { return _thumb; }
            set
            {
                Check();
                _thumb = value;
            }
        }

        /// <summary>How short the thumb may get, or null to leave it to the layers beneath.</summary>
        public float? LeastThumb
        {
            get { return _leastThumb; }
            set
            {
                Check();
                _leastThumb = value;
            }
        }

        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var scroll = element as Scroll;
            if (scroll == null)
                return;

            scroll.BarWidth = 0f;
            scroll.Thumb = Color.Transparent;
            scroll.LeastThumb = 0f;
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var scroll = element as Scroll;
            if (scroll == null)
                return;

            if (_barWidth.HasValue)
                scroll.BarWidth = _barWidth.Value;

            if (_thumb.HasValue)
                scroll.Thumb = _thumb.Value;

            if (_leastThumb.HasValue)
                scroll.LeastThumb = _leastThumb.Value;
        }

        /// <inheritdoc />
        internal override void Copy(ElementState other)
        {
            base.Copy(other);

            var state = other as ScrollState;
            if (state == null)
                return;

            _barWidth = state._barWidth;
            _thumb = state._thumb;
            _leastThumb = state._leastThumb;
        }
    }
}
