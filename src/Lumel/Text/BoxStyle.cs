using VRageMath;

namespace Lumel.Text
{
    /// <summary>What a host's box attribute asks for: each part of a box's look it sets, and nothing of what it leaves alone.</summary>
    /// <remarks>Where a box's lines sit is markdown-lite's own, by the attributes <c>left</c>, <c>center</c> and <c>right</c>.</remarks>
    public sealed class BoxStyle
    {
        /// <summary>
        /// What the attribute <paramref name="name" /> asks for: <paramref name="bar" /> down the box's edge, <paramref name="fill" />
        /// behind it, and <paramref name="color" /> for its plain words, each null to leave it alone.
        /// </summary>
        public BoxStyle(string name, Color? bar = null, Color? fill = null, Color? color = null)
        {
            Name = name;
            Bar = bar;
            Fill = fill;
            Color = color;
        }

        /// <summary>The attribute a box's fence names for this, as in <c>::: warning</c>.</summary>
        public string Name { get; }

        /// <summary>The color of a bar down the box's edge, setting it in as a quote is, or null for no bar.</summary>
        public Color? Bar { get; }

        /// <summary>The fill behind the box, or null for none.</summary>
        public Color? Fill { get; }

        /// <summary>The color of the box's plain words, or null for the text's own; strong, emphasized and linked words keep theirs.</summary>
        public Color? Color { get; }
    }
}
