using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A searchlight, a light on a turret mount, told how far to look, what to follow and whether to sweep while it has nothing.</summary>
    public sealed class Searchlight : Capability
    {
        // The start of the game's id for every action that picks a part of a ship to aim at, from its turret targeting controls.
        const string Aiming = "TargetingGroup_";

        const string Aims = "TargetingGroup_Selector";

        // The game's ids for the switches of what the light follows.
        static readonly string[] Followed =
        {
            "TargetMeteors", "TargetMissiles", "TargetSmallShips", "TargetLargeShips", "TargetCharacters", "TargetStations",
            "TargetNeutrals", "TargetFriends", "TargetEnemies"
        };

        // The same switches in Sprocket's words, in the order of Followed.
        static readonly string[] Called =
        {
            "meteors", "rockets", "small ships", "large ships", "characters", "stations", "neutrals", "friends", "enemies"
        };

        readonly List<Offer> _offered = new List<Offer>();

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySearchlight;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("Range", true, "AI aiming radius")
                .Measuring("Range", " m")
                .Renaming("EnableIdleMovement", true, "idle movement")
                .Renaming("EnableIdleMovement", false, "toggle idle movement")
                .Renaming(Aims, true, "aims at")
                .Renaming("EnableTargetLocking", true, "target locking")
                .Renaming("EnableTargetLocking", false, "toggle target locking")
                .Renaming("ViewRotation", true, "view rotation")
                .Hiding("Control", "FocusLockedTarget");

            for (var i = 0; i < Followed.Length; i++)
            {
                kind.Renaming(Followed[i], true, $"target {Called[i]}")
                    .Renaming(Followed[i], false, $"toggle target {Called[i]}");
            }

            HideAiming(kind, block);
        }

        /// <summary>Keeps every action that sets or steps what the light aims at out of the list, the value saying them all.</summary>
        void HideAiming(EntityKind kind, IMyTerminalBlock block)
        {
            _offered.Clear();
            Toolbars.Offered(block, _offered);
            foreach (var offer in _offered)
            {
                var id = offer.Id;
                if (id != null && id != Aims && id.StartsWith(Aiming, StringComparison.Ordinal))
                    kind.Hiding(id);
            }
        }
    }
}
