using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>Blocks in the order a person reads them: by name, ignoring case, then by id; a block that is gone comes last.</summary>
    public sealed class NameOrder : IComparer<GridEntity>
    {
        /// <inheritdoc />
        public int Compare(GridEntity x, GridEntity y)
        {
            if (x == null || y == null)
                return x == null ? y == null ? 0 : 1 : -1;

            var byName = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            return byName != 0 ? byName : x.Id.CompareTo(y.Id);
        }
    }
}
