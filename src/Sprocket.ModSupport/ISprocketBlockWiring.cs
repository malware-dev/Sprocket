namespace Sprocket.ModSupport
{
    /// <summary>What the wrapper does to a scope to say which blocks it is.</summary>
    /// <remarks>The wrapper's own plumbing, not for mods: a mod reaches it only by casting.</remarks>
    public interface ISprocketBlockWiring
    {
        /// <summary>Says which blocks a base scope is: of the object builder type <paramref name="blockType" />, of <paramref name="subtypes" /> where any are given.</summary>
        SprocketBlock OfType(string blockType, string[] subtypes);
    }
}
