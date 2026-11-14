using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Players
{
    /// <summary>Whether a player may use a block by its owner and share setting, and whether an admin uses every terminal.</summary>
    /// <remarks>
    /// The two are asked apart, since the game's own access test lets in an admin using every terminal, which would make a
    /// ship taken from an admin look shared with them.
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

        /// <summary>Whether <paramref name="player" /> is an admin with the game's admin setting to use every terminal turned on.</summary>
        public static bool UsesEveryTerminal(long player)
        {
            var steamId = MyAPIGateway.Players?.TryGetSteamId(player) ?? 0;
            MyAdminSettingsEnum settings;
            return steamId != 0 && MyAPIGateway.Session != null && MyAPIGateway.Session.TryGetAdminSettings(steamId, out settings)
                && (settings & MyAdminSettingsEnum.UseTerminals) != 0;
        }
    }
}
