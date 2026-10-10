using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A searchlight: a light on a turret mount that finds and follows targets itself, told how far to look, what to follow
    /// and whether to sweep while it has nothing.
    /// </summary>
    /// <remarks>
    /// Its colors and blinking are a light's - see <see cref="Light" />. Whether it has found a target is kept on the
    /// searchlight's own class, out of a mod's reach, so it offers nothing to follow one with.
    /// </remarks>
    public sealed class Searchlight : Capability
    {
        /// <summary>What every action that picks a part of a ship to aim at begins with, one per targeting group the game has.</summary>
        /// <remarks>The game's own ids, from its turret targeting controls; the selector beside them is the value that says it.</remarks>
        const string Aiming = "TargetingGroup_";

        const string Aims = "TargetingGroup_Selector";

        /// <summary>The switches for what the light follows, by the game's ids.</summary>
        static readonly string[] Followed =
        {
            "TargetMeteors", "TargetMissiles", "TargetSmallShips", "TargetLargeShips", "TargetCharacters", "TargetStations",
            "TargetNeutrals", "TargetFriends", "TargetEnemies"
        };

        /// <summary>The same switches in Sprocket's words, in the same order.</summary>
        static readonly string[] Called =
        {
            "meteors", "missiles", "small ships", "large ships", "characters", "stations", "neutrals", "friends", "enemies"
        };

        readonly List<Offer> _offered = new List<Offer>();

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySearchlight;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Taking control of the light is something a person does. Focusing on a locked target does nothing on a searchlight,
        /// which ignores it.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("Range", true, "range")
                .Renaming("EnableIdleMovement", true, "moves when idle")
                .Renaming("EnableIdleMovement", false, "toggle moves when idle")
                .Renaming(Aims, true, "aims at")
                .Renaming("EnableTargetLocking", true, "target locking allowed")
                .Renaming("EnableTargetLocking", false, "toggle target locking allowed")
                .Renaming("ViewRotation", true, "view rotation")
                .Hiding("Control", "FocusLockedTarget");

            for (var i = 0; i < Followed.Length; i++)
            {
                kind.Renaming(Followed[i], true, "targets " + Called[i])
                    .Renaming(Followed[i], false, "toggle targets " + Called[i]);
            }

            HideAiming(kind, block);
        }

        /// <summary>Keeps every action that sets or steps what the light aims at out of the list, the value saying them all.</summary>
        /// <remarks>Read off the block, because the game makes one for every targeting group it has, a mod's included.</remarks>
        void HideAiming(EntityKind kind, IMyTerminalBlock block)
        {
            _offered.Clear();
            Toolbars.Offered(block, _offered);
            for (var i = 0; i < _offered.Count; i++)
            {
                var id = _offered[i].Id;
                if (id != null && id != Aims && id.StartsWith(Aiming, StringComparison.Ordinal))
                    kind.Hiding(id);
            }
        }
    }
}
