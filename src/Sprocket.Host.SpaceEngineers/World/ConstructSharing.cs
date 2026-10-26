using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Who a construct is shared with, as its big owners last set it on a block they own.</summary>
    /// <remarks>
    /// Read off a block a big owner owns and kept through a change of hands, because the game turns the sharing of every
    /// block given to another player off.
    /// </remarks>
    public sealed class ConstructSharing
    {
        // An identity no player has, which the game's access test lets in only where a block is shared with everyone.
        const long Stranger = long.MaxValue;

        long _owner;
        long _faction;
        bool _withFaction;
        bool _withEveryone;

        /// <summary>Whether the sharing has been read, which it is as soon as the construct has a big owner.</summary>
        public bool IsKnown { get; private set; }

        /// <summary>Reads the sharing off <paramref name="block" />, a block one of the big owners owns.</summary>
        /// <remarks>
        /// Sharing with a faction can only be read where its owner's faction has another member, so a lone member's sharing
        /// is read again as somebody joins.
        /// </remarks>
        public void Read(IMyTerminalBlock block)
        {
            IsKnown = true;
            _owner = block.OwnerId;
            _withEveryone = Access.Shared(block, Stranger);

            IMyFaction faction = FactionOf(_owner);
            _faction = faction == null ? 0 : faction.FactionId;
            _withFaction = false;
            if (faction == null)
                return;

            foreach (var member in faction.Members.Keys)
            {
                if (member == _owner)
                    continue;

                _withFaction = Access.Shared(block, member);
                return;
            }
        }

        /// <summary>Whether <paramref name="player" /> may use the construct: the owner of the block read, or anyone it is shared with.</summary>
        public bool Lets(long player)
        {
            if (player == _owner || _withEveryone)
                return true;

            if (!_withFaction)
                return false;

            IMyFaction faction = FactionOf(player);
            return faction != null && faction.FactionId == _faction;
        }

        /// <summary>The faction <paramref name="player" /> is in, or null where they are in none or the session has no factions.</summary>
        static IMyFaction FactionOf(long player)
        {
            IMyFactionCollection factions = MyAPIGateway.Session?.Factions;
            return factions?.TryGetPlayerFaction(player);
        }
    }
}
