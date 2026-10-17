using VRageMath;

namespace Lumel
{
    /// <summary>An element that draws words, whose face, size and color a style can set.</summary>
    public interface ITextual
    {
        /// <summary>Which face the words are drawn in.</summary>
        string Font { get; set; }

        /// <summary>How tall a line of the words is, in UI units.</summary>
        float FontSize { get; set; }

        /// <summary>The color the words are drawn in.</summary>
        Color Color { get; set; }
    }
}
