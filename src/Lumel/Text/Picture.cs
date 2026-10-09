namespace Lumel.Text
{
    /// <summary>What an image in a document shows: the locator its picture is found by, how big it is written to be drawn, and how far it is turned.</summary>
    public sealed class Picture
    {
        /// <summary>The picture <paramref name="locator" /> leads to, drawn <paramref name="width" /> wide and <paramref name="height" /> tall where they are numbers, and turned <paramref name="rotation" /> degrees clockwise.</summary>
        public Picture(string locator, float width, float height, float rotation)
        {
            Locator = locator;
            Width = width;
            Height = height;
            Rotation = rotation;
        }

        /// <summary>The locator, as written: what it leads to is for the host's resolver to say.</summary>
        public string Locator { get; }

        /// <summary>How wide it is drawn, or <c>float.NaN</c> where that is not written.</summary>
        public float Width { get; }

        /// <summary>How tall it is drawn, or <c>float.NaN</c> where that is not written.</summary>
        public float Height { get; }

        /// <summary>How far it is turned clockwise, in degrees as written: 0 where no turn is written.</summary>
        public float Rotation { get; }
    }
}
