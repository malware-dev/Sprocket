namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How a window looks.</summary>
    public sealed class WindowStyle : Style<WindowState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public WindowStyle()
        {
        }

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
        public WindowStyle(WindowStyle basedOn) : base(basedOn)
        {
        }
    }
}
