using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Grids
{
    // What the harness asks of a block and the mod does not. A .debug.cs file, compiled into local builds only and never
    // packed for the game.
    public sealed partial class GridEntity
    {
        /// <summary>How many ticks this block should take to finish <paramref name="actionId" /> with <paramref name="arguments" />, or <see cref="Unestimated" />.</summary>
        /// <remarks>Works out what the action was told on every call.</remarks>
        public int Expected(string actionId, IList<Literal> arguments)
        {
            var bound = Bind(actionId, arguments);
            return bound?.Expected(_block) ?? Unestimated;
        }
    }
}
