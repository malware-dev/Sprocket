using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>The game's tree of types of block, as the kinds of block on a construct show it.</summary>
    public static class TypeTree
    {
        /// <summary>
        /// Whether a slot of <paramref name="type" />, empty for any block, takes less than <paramref name="part" /> takes now: it
        /// stands below the slot's type in the tree, as a kind of block in <paramref name="world" /> holding both has them, or
        /// the tree cannot say.
        /// </summary>
        public static bool Narrows(GridWorld world, Part part, string type)
        {
            if (type.Length == 0 || type == part.Expects)
                return false;

            if (part.Expects.Length == 0)
                return true;

            foreach (var entity in world.Entities)
            {
                var lineage = entity.Kind.Lineage;
                var at = lineage.IndexOf(part.Expects);
                if (at >= 0)
                    return lineage.IndexOf(type) < at;
            }

            return true;
        }
    }
}
