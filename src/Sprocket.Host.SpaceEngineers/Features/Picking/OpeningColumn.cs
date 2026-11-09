
namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// Which column a picker opens on: the one standing for the part pressed to open it, with the
    /// caret in the first thing in it to type into.
    /// </summary>
    public enum OpeningColumn
    {
        /// <summary>The blocks, and the box that narrows them.</summary>
        Blocks,

        /// <summary>What happens to the blocks or is done to them, and the box that narrows it.</summary>
        What,

        /// <summary>What is given to what is done, its options or the new value, and the first of them.</summary>
        Arguments,

        /// <summary>The selector of the slots an option names, as the part naming them stands for; the pick finishes with what is selected there.</summary>
        Target,

        /// <summary>No column in particular, where the part pressed stands for one with nothing to type into.</summary>
        Nothing
    }
}
