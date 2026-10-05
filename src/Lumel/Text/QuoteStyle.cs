using VRageMath;

namespace Lumel.Text
{
    /// <summary>How a quote with one tag looks: each part it sets in place of a plain quote's, and the plain quote's look for the rest.</summary>
    public sealed class QuoteStyle
    {
        /// <summary>The look of quotes tagged <paramref name="tag" />: <paramref name="bar" /> down their edge, <paramref name="fill" /> behind them, and <paramref name="color" /> for their plain words, each null for a plain quote's.</summary>
        public QuoteStyle(string tag, Color? bar = null, Color? fill = null, Color? color = null)
        {
            Tag = tag;
            Bar = bar;
            Fill = fill;
            Color = color;
        }

        /// <summary>The tag a quote's first line names, as <c>[!tag]</c>, for this look.</summary>
        public string Tag { get; }

        /// <summary>The color of the bar down the quote's edge, or null for the rule color.</summary>
        public Color? Bar { get; }

        /// <summary>The fill behind the quote, or null for none.</summary>
        public Color? Fill { get; }

        /// <summary>The color of the quote's plain words, or null for the text's own; strong, emphasized and linked words keep theirs.</summary>
        public Color? Color { get; }
    }
}
