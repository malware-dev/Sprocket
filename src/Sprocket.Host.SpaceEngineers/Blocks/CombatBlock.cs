using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An offensive or defensive combat block: who it goes after, and whether it has found someone.</summary>
    /// <remarks>
    /// Finding and losing a target is something the block does of its own accord, so it is offered as a pair of signals
    /// beside the value.
    /// </remarks>
    public sealed class CombatBlock : Capability
    {
        static readonly Reading Found = Reading.Of<IMyTerminalBlock>(block =>
        {
            var search = Search(block);
            return search != null && search.FoundEnemy != null;
        });

        static readonly OwnValue Targeting = OwnValue.Flag(Found);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyOffensiveCombatBlock || block is IMyDefensiveCombatBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("targeting", "targeting", Targeting).Status().Announced()
                .WithChange("finds-a-target", "finds a target", "targeting", Literal.Of(true)).Announced()
                .Reading("is targeting", "is not targeting")
                .WithChange("loses-its-target", "loses its target", "targeting", Literal.Of(false)).Announced()
                .Reading("is not targeting", "is targeting")
                .Renaming("TargetLock", true, block is IMyDefensiveCombatBlock ? "defend against" : "attack target")
                .Renaming("TargetingGroup", true, "aims at")
                .Renaming("CanTargetCharacters", true, "target characters")
                .Renaming("CanTargetCharacters", false, "toggle target characters")
                .Hiding("SetAttackMode_EnemiesOnly", "SetAttackMode_EnemiesAndNeutrals", "SetTargetingGroup_Weapons",
                    "SetTargetingGroup_Propulsion", "SetTargetingGroup_PowerSystems");
        }

        /// <summary>Told as the block's search finds a target or loses one.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var search = Search(block);
            return search == null ? null : new Listening(search, told);
        }

        /// <summary>The block's search for a target, or null where it has none.</summary>
        static IMySearchEnemyComponent Search(IMyTerminalBlock block)
        {
            var offensive = block as IMyOffensiveCombatBlock;
            if (offensive != null)
                return offensive.SearchEnemyComponent;

            var defensive = block as IMyDefensiveCombatBlock;
            return defensive?.SearchEnemyComponent;
        }

        /// <summary>A search being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMySearchEnemyComponent _search;
            readonly Action _told;

            public Listening(IMySearchEnemyComponent search, Action told)
            {
                _search = search;
                _told = told;
                _search.TargetChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _search.TargetChanged -= Changed;
            }

            void Changed(IMyEntity was, IMyEntity now, bool locked)
            {
                _told();
            }
        }
    }
}
