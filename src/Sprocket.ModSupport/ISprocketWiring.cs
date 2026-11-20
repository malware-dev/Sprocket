namespace Sprocket.ModSupport
{
    /// <summary>The key to the constructors of the wrapper's own types: only the wrapper holds one, so only the wrapper makes them.</summary>
    /// <remarks>The wrapper's own plumbing, not for mods: a mod gets its objects from <see cref="SprocketModSupport" />, <see cref="Option" /> and the builder.</remarks>
    public interface ISprocketWiring
    {
    }
}
