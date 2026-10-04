namespace Lumel.Text
{
    /// <summary>An image among a block's words: the picture it shows, and its description as its words.</summary>
    /// <remarks>Where the picture is not to be found, the description is said in its place.</remarks>
    public sealed class ImageRun : Run
    {
        /// <summary>An image of <paramref name="picture" />, described as <paramref name="words" />, which are set apart as <paramref name="kind" /> says.</summary>
        public ImageRun(Picture picture, string words, RunKind kind)
            : base(words, kind)
        {
            Picture = picture;
        }

        /// <summary>What the image shows.</summary>
        public Picture Picture { get; }
    }
}
