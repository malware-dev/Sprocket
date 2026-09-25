namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How a document looks.</summary>
    public sealed class RichTextStyle : Style<RichTextState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public RichTextStyle()
        {
        }

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
        public RichTextStyle(RichTextStyle basedOn) : base(basedOn)
        {
        }
    }
}
