using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>How a run of text looks: its face, its size and its color, beside what any element has.</summary>
    public sealed class TextStyle : Style<TextState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public TextStyle()
        {
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public TextStyle(TextStyle basedOn) : base(basedOn)
        {
        }
    }
}
