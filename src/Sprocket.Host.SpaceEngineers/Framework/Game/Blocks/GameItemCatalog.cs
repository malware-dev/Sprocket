using System.Collections.Generic;
using Sandbox.Definitions;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>The items the game defines, as its definition manager lists its physical items.</summary>
    public sealed class GameItemCatalog : IItemCatalog
    {
        /// <inheritdoc />
        public void Public(List<string> names, List<string> ids)
        {
            var definitions = MyDefinitionManager.Static;
            if (definitions == null)
                return;

            foreach (var item in definitions.GetPhysicalItemDefinitions())
            {
                if (!item.Public)
                    continue;

                names.Add(item.DisplayNameText);
                ids.Add(ItemIds.Said(item.Id));
            }
        }
    }
}
