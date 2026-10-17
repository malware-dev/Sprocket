using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>Members in the order of the terminal's names for them.</summary>
    public sealed class TerminalNameOrder : IComparer<EntityAction>
    {
        /// <inheritdoc />
        public int Compare(EntityAction left, EntityAction right)
        {
            return string.Compare(left == null ? "" : left.Declared, right == null ? "" : right.Declared,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
