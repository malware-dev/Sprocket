namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How a run of text looks: its face, its size and its colour, beside what any element has.</summary>
    public sealed class TextStyle : Style<TextState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public TextStyle()
        {
        }

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
        public TextStyle(TextStyle basedOn) : base(basedOn)
        {
        }
    }
}
