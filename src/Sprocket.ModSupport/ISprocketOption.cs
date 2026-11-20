using System.Collections.Generic;

namespace Sprocket.ModSupport
{
    /// <summary>An option of any type, as a member lists the options it takes.</summary>
    public interface ISprocketOption
    {
        /// <summary>The option as Sprocket reads it, by the keys of <see cref="SprocketWire" />.</summary>
        Dictionary<string, object> Wire();
    }
}
