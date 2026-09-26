using Sprocket.Host.SpaceEngineers.Ui.Styling;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>How a document looks.</summary>
    public sealed class RichTextStyle : Style<RichTextState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public RichTextStyle()
        {
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public RichTextStyle(RichTextStyle basedOn) : base(basedOn)
        {
        }
    }
}
