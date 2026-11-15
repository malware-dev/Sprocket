namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    /// <summary>A row of a <see cref="CategoryList" /> that may stand under a category.</summary>
    public interface IStandsIn
    {
        /// <summary>Whether it stands under a category, set in from the grip.</summary>
        bool Indented { get; }

        /// <summary>
        /// Whether the list sets the whole row in while it is <see cref="Indented" />, so a spine runs beside it rather than
        /// through it: a row drawn as a box. A row that is not sets in what it holds itself.
        /// </summary>
        bool SetsInWhole { get; }
    }
}
