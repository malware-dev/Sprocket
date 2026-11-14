using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>The facets of a type the host provides that a set of blocks share, and what one of them is doing now.</summary>
    public static class Facets
    {
        /// <summary>The names of the facets of <paramref name="type" /> every one of <paramref name="blocks" /> has, in the first block's order.</summary>
        public static List<string> Shared(GridWorld world, IList<long> blocks, HostType type)
        {
            var shared = new List<string>();
            for (var i = 0; i < blocks.Count; i++)
            {
                GridEntity entity = world.FindEntity(blocks[i]);
                if (entity == null)
                    continue;

                var facets = entity.Kind.Facets(type);
                if (i == 0)
                {
                    foreach (var facet in facets)
                        shared.Add(facet.Name);

                    continue;
                }

                shared.RemoveAll(name => !Has(facets, name));
            }

            return shared;
        }

        static bool Has(IList<HostTypeFacet> facets, string name)
        {
            foreach (var facet in facets)
            {
                if (facet.Name == name)
                    return true;
            }

            return false;
        }

        /// <summary>What the facet called <paramref name="facet" /> of <paramref name="type" /> is doing now on the first of <paramref name="blocks" />.</summary>
        public static string Showing(GridWorld world, IList<long> blocks, HostType type, string facet)
        {
            GridEntity entity = blocks.Count == 0 ? null : world.FindEntity(blocks[0]);
            if (entity == null)
                return "";

            foreach (var one in entity.Kind.Facets(type))
            {
                if (one.Name == facet)
                    return type.Showing(entity.Block, one.Number);
            }

            return "";
        }
    }
}
