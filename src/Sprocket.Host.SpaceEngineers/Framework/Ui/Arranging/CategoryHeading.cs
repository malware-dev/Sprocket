using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>A category's name in a list of a system's things, such as its slots or sequences, over the run under it, or over none.</summary>
    /// <remarks>Two headings are the same row where they head the same run, by its first one, or the same empty category.</remarks>
    public sealed class CategoryHeading
    {
        CategoryHeading(string name, ICategorized first, EmptyCategory empty)
        {
            Name = name;
            First = first;
            Empty = empty;
        }

        /// <summary>What the category is called.</summary>
        public string Name { get; }

        /// <summary>The first of the run it heads, or null where it heads none.</summary>
        public ICategorized First { get; }

        /// <summary>The empty category it is, or null where it heads a run.</summary>
        public EmptyCategory Empty { get; }

        /// <summary>The heading over the run <paramref name="first" /> starts, named for its category.</summary>
        public static CategoryHeading Over(ICategorized first)
        {
            return new CategoryHeading(first.Category, first, null);
        }

        /// <summary>The heading of <paramref name="empty" />, over nothing.</summary>
        public static CategoryHeading Of(EmptyCategory empty)
        {
            return new CategoryHeading(empty.Name, null, empty);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var other = obj as CategoryHeading;
            return other != null && other.First == First && other.Empty == Empty;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Empty != null ? Empty.GetHashCode() : First.GetHashCode();
        }
    }
}
