using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A defensive combat block: fleeing, how it flees, and how it defends itself.</summary>
    /// <remarks>
    /// Flee is the one a programmable block may call, which flees only from a target the block has found; the game's
    /// forced flee, offered to mods alone, flees from nothing and fails where it is to flee away from a target it has not
    /// found. Starting and stopping fleeing is something the block does of its own accord, so it is offered as a pair of
    /// signals beside the value.
    /// </remarks>
    public sealed class DefensiveCombat : Capability
    {
        static readonly Reading Away = Reading.Of<IMyDefensiveCombatBlock>(block => block.IsFleeing);

        static readonly Being Fleeing = Being.Flag(Away);

        static readonly Doing Flees = Doing.Of<IMyDefensiveCombatBlock>(block => block.Flee());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyDefensiveCombatBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction("flee", "flee", Flees).In(Category.Automation)
                .WithValue("fleeing", "fleeing", Fleeing).Status().Announced()
                .WithChange("starts-fleeing", "starts fleeing", "fleeing", Literal.Of(true)).Announced()
                .Reading("is fleeing", "is not fleeing")
                .WithChange("stops-fleeing", "stops fleeing", "fleeing", Literal.Of(false)).Announced()
                .Reading("is not fleeing", "is fleeing")
                .Renaming("Flee", true, "flees when")
                .Renaming("FleeMode", true, "flee destination")
                .Renaming("FleeDistance", true, "flee distance")
                .Measuring("FleeDistance", " m")
                .Renaming("WaypointZoneSize", true, "flee waypoint size")
                .Measuring("WaypointZoneSize", " m")
                .Renaming("LockTarget", true, "lock target")
                .Renaming("LockTarget", false, "toggle lock target")
                .Renaming("EvasiveManeuvers", true, "evasive maneuvers")
                .Renaming("EvasiveManeuvers", false, "toggle evasive maneuvers")
                .Renaming("EvasiveManeuverAngle", true, "evasive angle")
                .Measuring("EvasiveManeuverAngle", "°")
                .Renaming("EvasiveManeuverIntervalRange", true, "evasive interval")
                .Measuring("EvasiveManeuverIntervalRange", " s");
        }

        /// <summary>Told as the block starts or stops fleeing, which it says itself.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var defensive = block as IMyDefensiveCombatBlock;
            return defensive == null ? null : new Listening(defensive, told);
        }

        /// <summary>A defensive block being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyDefensiveCombatBlock _block;
            readonly Action _told;

            public Listening(IMyDefensiveCombatBlock block, Action told)
            {
                _block = block;
                _told = told;
                _block.IsFleeingChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.IsFleeingChanged -= Changed;
            }

            void Changed(IMyDefensiveCombatBlock block, bool fleeing)
            {
                _told();
            }
        }
    }
}
