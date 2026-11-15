namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>
    /// The end of a category in a list being arranged: a row under what the category holds, where something new is added into
    /// it, and above which something carried lands in it.
    /// </summary>
    /// <remarks>Two ends are the same row where they end the same category.</remarks>
    public sealed class CategoryEnd
    {
        /// <summary>The end of the category <paramref name="heading" /> heads.</summary>
        public CategoryEnd(CategoryHeading heading)
        {
            Heading = heading;
        }

        /// <summary>The heading of the category it ends.</summary>
        public CategoryHeading Heading { get; }

        /// <summary>What the category is called.</summary>
        public string Name => Heading.Name;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as CategoryEnd;
            return other != null && other.Heading.Equals(Heading);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Heading.GetHashCode() ^ 0x5bd1e995;
        }
    }
}
