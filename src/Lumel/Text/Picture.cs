namespace Lumel.Text
{
    /// <summary>What an image in a document shows: the locator its picture is found by, and how big it is written to be drawn.</summary>
    public sealed class Picture
    {
        /// <summary>The picture <paramref name="locator" /> leads to, drawn <paramref name="width" /> wide and <paramref name="height" /> tall where they are numbers.</summary>
        public Picture(string locator, float width, float height)
        {
            Locator = locator;
            Width = width;
            Height = height;
        }

        /// <summary>The locator, as written: what it leads to is for the host's resolver to say.</summary>
        public string Locator { get; }

        /// <summary>How wide it is drawn, or <c>float.NaN</c> where that is not written.</summary>
        public float Width { get; }

        /// <summary>How tall it is drawn, or <c>float.NaN</c> where that is not written.</summary>
        public float Height { get; }
    }
}
