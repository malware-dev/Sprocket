using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Styling;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
{
    /// <summary>What a window looks like in one state, on top of what any element does.</summary>
    public class WindowState : ElementState
    {
        /// <summary>What a modal window paints over everything beneath it, or null to leave it to the layers beneath.</summary>
        public Color? Backdrop { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var window = element as Window;
            if (window != null)
                window.Backdrop = Color.Transparent;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var window = element as Window;
            if (window != null && Backdrop.HasValue)
                window.Backdrop = Backdrop.Value;
        }
    }
}
