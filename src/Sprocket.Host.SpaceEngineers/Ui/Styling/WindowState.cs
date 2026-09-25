using Sprocket.Host.SpaceEngineers.Ui.Windows;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a window looks like in one state, on top of what any element does.</summary>
    public class WindowState : ElementState
    {
        Color? _backdrop;

        /// <summary>What a modal window paints over everything beneath it, or null to leave it to the layers beneath.</summary>
        public Color? Backdrop
        {
            get { return _backdrop; }
            set
            {
                Check();
                _backdrop = value;
            }
        }

        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var window = element as Window;
            if (window != null)
                window.Backdrop = Color.Transparent;
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var window = element as Window;
            if (window != null && _backdrop.HasValue)
                window.Backdrop = _backdrop.Value;
        }

        /// <inheritdoc />
        internal override void Copy(ElementState other)
        {
            base.Copy(other);

            var state = other as WindowState;
            if (state != null)
                _backdrop = state._backdrop;
        }
    }
}
