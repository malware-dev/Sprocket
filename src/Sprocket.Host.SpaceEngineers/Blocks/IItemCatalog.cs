using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The items the game defines, which only the game can list.</summary>
    public interface IItemCatalog
    {
        /// <summary>Adds each public item, in the game's order, its shown name to <paramref name="names" /> and how a step names it to <paramref name="ids" />.</summary>
        void Public(List<string> names, List<string> ids);
    }
}
