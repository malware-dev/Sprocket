namespace Lumel.Text
{
    /// <summary>What an image in a document shows: the picture's name, and how big it is written to be drawn.</summary>
    public sealed class Picture
    {
        /// <summary>The picture called <paramref name="name" />, drawn <paramref name="width" /> wide and <paramref name="height" /> tall where they are numbers.</summary>
        public Picture(string name, float width, float height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        /// <summary>The name the picture is found by.</summary>
        public string Name { get; }

        /// <summary>How wide it is drawn, or <c>float.NaN</c> where that is not written.</summary>
        public float Width { get; }

        /// <summary>How tall it is drawn, or <c>float.NaN</c> where that is not written.</summary>
        public float Height { get; }
    }
}
