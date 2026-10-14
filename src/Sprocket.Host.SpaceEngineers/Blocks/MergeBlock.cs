using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

// The state lives in the Ingame namespace, which cannot be imported whole: IMyShipMergeBlock is declared in both, and the
// mod-side one is the one with the event on it.
using MergeState = SpaceEngineers.Game.ModAPI.Ingame.MergeState;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A merge block: how far it has got with joining two ships into one.</summary>
    /// <remarks>
    /// It merges while switched on and lets go when switched off, so the switch every block has is its merge and release.
    /// <para>
    /// Everything is told by the merge state changing, which the game raises on the server alone. Its event for a merge
    /// about to happen is not used: the game cannot take a handler for it back.
    /// </para>
    /// </remarks>
    public sealed class MergeBlock : Capability
    {
        static readonly string[] Statuses = { "merged", "ready-to-merge", "nothing-in-reach", "not-working" };

        static readonly Being State = Being.OneOf<IMyShipMergeBlock>(Statuses, merge => Position(merge.State));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipMergeBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("status", "status", State).Status().Announced().In(Category.Locking)
                .WithChange("becomes-ready-to-merge", "becomes ready to merge", "status", Literal.Name("ready-to-merge"))
                .Announced().Reading("is ready to merge", "is not ready to merge")
                .WithChange("merges", "merges", "status", Literal.Name("merged")).Announced()
                .Reading("is merged", "is not merged");
        }

        /// <summary>Told as the merge block's state changes, which the game raises on the server.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var merge = block as IMyShipMergeBlock;
            return merge == null ? null : new Listening(merge, told);
        }

        /// <summary>Where <paramref name="state" /> stands among the merge block's statuses.</summary>
        /// <remarks>
        /// Constrained is another merge block in reach, its magnets pulling the two into line; working is switched on with
        /// none near; none, switched off or damaged, and unset read the same.
        /// </remarks>
        static int Position(MergeState state)
        {
            switch (state)
            {
                case MergeState.Locked:
                    return 0;
                case MergeState.Constrained:
                    return 1;
                case MergeState.Working:
                    return 2;
            }

            return 3;
        }

        /// <summary>A merge block being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyShipMergeBlock _merge;
            readonly Action _told;

            public Listening(IMyShipMergeBlock merge, Action told)
            {
                _merge = merge;
                _told = told;
                _merge.MergeStateChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _merge.MergeStateChanged -= Changed;
            }

            void Changed(IMyShipMergeBlock merge)
            {
                _told();
            }
        }
    }
}
