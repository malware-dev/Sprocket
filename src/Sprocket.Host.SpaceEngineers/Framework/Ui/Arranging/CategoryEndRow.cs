using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Controls;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>
    /// The end of a category in a <see cref="CategoryList" />: the way to add something new into it, set in under what it holds,
    /// where the category's spine turns in. It is never carried; something carried lands in the category above it.
    /// </summary>
    public partial class CategoryEndRow : HStack, IReorderable, IDragged, IStandsIn
    {
        /// <summary>An end row with nothing to add yet.</summary>
        public CategoryEndRow()
        {
            CreateView();
        }

        /// <summary>The way to add something new into the category.</summary>
        public PlusMenu Adds => _adds;

        /// <summary>What the category it ends is called.</summary>
        public string Category { get; set; } = "";

        /// <summary>Always: it stands under its category.</summary>
        public bool Indented => true;

        /// <summary>The whole row, as the rows above it are set in.</summary>
        public bool SetsInWhole => true;

        /// <inheritdoc />
        public DragHandle DragHandle => _grip;

        /// <summary>Never: it is not carried.</summary>
        public bool IsDragging => false;

        /// <inheritdoc />
        bool IDragged.IsDragging
        {
            set { }
        }

        /// <summary>None: it is not carried.</summary>
        public int DraggedWithCount => 0;
    }
}
