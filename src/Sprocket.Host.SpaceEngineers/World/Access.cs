using Sandbox.ModAPI;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Whether a player may use a block, judged by its owner and share setting alone.</summary>
    /// <remarks>
    /// The game's own access test also lets in an admin using every terminal, which would make a ship taken from an admin
    /// look shared with them.
    /// </remarks>
    public static class Access
    {
        /// <summary>Whether <paramref name="player" /> may use <paramref name="block" />: its owner, whoever it is shared with, or anybody where nobody owns it.</summary>
        public static bool Shared(IMyTerminalBlock block, long player)
        {
            var relation = block.GetUserRelationToOwner(player);
            return relation == MyRelationsBetweenPlayerAndBlock.NoOwnership || relation == MyRelationsBetweenPlayerAndBlock.Owner
                || relation == MyRelationsBetweenPlayerAndBlock.FactionShare;
        }
    }
}
