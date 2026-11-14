using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>A category's name on the installation panel, over the run of slots under it, or over none.</summary>
    /// <remarks>Two headings are the same row where they head the same run, by its first slot, or the same empty category.</remarks>
    public sealed class CategoryHeading
    {
        CategoryHeading(string name, Part first, EmptyCategory empty)
        {
            Name = name;
            First = first;
            Empty = empty;
        }

        /// <summary>What the category is called.</summary>
        public string Name { get; }

        /// <summary>The first slot of the run it heads, or null where it heads none.</summary>
        public Part First { get; }

        /// <summary>The empty category it is, or null where it heads a run of slots.</summary>
        public EmptyCategory Empty { get; }

        /// <summary>The heading over the run of slots <paramref name="first" /> starts, named for its category.</summary>
        public static CategoryHeading Over(Part first)
        {
            return new CategoryHeading(first.Category, first, null);
        }

        /// <summary>The heading of <paramref name="empty" />, over no slot.</summary>
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
