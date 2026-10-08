using VRageMath;
using Lumel.Styling;

namespace Lumel.Windows
{
    /// <summary>What a window looks like in one state, on top of what any element does.</summary>
    public class WindowState : ElementState
    {
        /// <summary>What a modal window paints over everything beneath it, or null to leave it to the layers beneath.</summary>
        public Color? Backdrop { get; set; }

        /// <summary>What the window casts on what lies beneath it, or null to leave it to the layers beneath.</summary>
        public Shadow Shadow { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var window = element as Window;
            if (window == null)
                return;

            if (Backdrop.HasValue)
                window.Backdrop = Color.Transparent;

            if (Shadow != null)
                window.Shadow = null;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var window = element as Window;
            if (window == null)
                return;

            if (Backdrop.HasValue)
                window.Backdrop = Backdrop.Value;

            if (Shadow != null)
                window.Shadow = Shadow;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            Backdrop = null;
            Shadow = null;
        }

        /// <inheritdoc />
        public override void Gather(ElementState layer)
        {
            base.Gather(layer);
            var other = layer as WindowState;
            if (other == null)
                return;

            if (!Backdrop.HasValue)
                Backdrop = other.Backdrop;

            if (Shadow == null)
                Shadow = other.Shadow;
        }
    }
}
