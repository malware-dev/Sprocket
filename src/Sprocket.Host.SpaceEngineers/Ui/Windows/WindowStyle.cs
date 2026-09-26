using Sprocket.Host.SpaceEngineers.Ui.Styling;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
{
    /// <summary>How a window looks.</summary>
    public sealed class WindowStyle : Style<WindowState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public WindowStyle()
        {
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public WindowStyle(WindowStyle basedOn) : base(basedOn)
        {
        }
    }
}
