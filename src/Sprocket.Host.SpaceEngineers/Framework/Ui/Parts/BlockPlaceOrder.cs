using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>Places in a list of blocks, by the blocks in them; two blocks that are both gone keep the order they were picked in.</summary>
    public sealed class BlockPlaceOrder : IComparer<int>
    {
        readonly List<GridEntity> _entities;

        /// <summary>An order of places in <paramref name="entities" />.</summary>
        public BlockPlaceOrder(List<GridEntity> entities)
        {
            _entities = entities;
        }

        /// <inheritdoc />
        public int Compare(int x, int y)
        {
            var byBlock = SentenceText.ByName.Compare(_entities[x], _entities[y]);
            return byBlock != 0 ? byBlock : x.CompareTo(y);
        }
    }
}
