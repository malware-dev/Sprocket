
namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// Which column a picker opens on: the one standing for the part pressed to open it, with the
    /// caret in the first thing in it to type into.
    /// </summary>
    public enum Opening
    {
        /// <summary>The blocks, and the box that narrows them.</summary>
        Blocks,

        /// <summary>What happens to the blocks or is done to them, and the box that narrows it.</summary>
        What,

        /// <summary>The values what is done is given, and the first of them.</summary>
        Values,

        /// <summary>No column in particular, where the part pressed stands for one with nothing to type into.</summary>
        Nothing
    }
}
