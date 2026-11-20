using System.Collections.Generic;

namespace Sprocket.ModSupport
{
    /// <summary>Something the wrapper tells Sprocket about as a dictionary.</summary>
    /// <remarks>The wrapper's own plumbing, not for mods: a mod reaches it only by casting.</remarks>
    public interface ISprocketWired
    {
        /// <summary>The thing as Sprocket reads it, by the keys of <see cref="SprocketWire" />.</summary>
        Dictionary<string, object> Wire();
    }
}
