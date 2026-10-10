using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>One of the five AI blocks, flight, task, combat or path: starting and stopping its AI, and whether it runs.</summary>
    /// <remarks>
    /// The game's own on and off for the AI is the order a sequence gives, and whether the AI runs is the game's answer,
    /// which goes off by itself as well: one AI block of each kind runs on a grid, and starting one stops the other. So the
    /// order is offered as its two actions and its toggle, its value is left out, and whether the AI runs is Sprocket's
    /// own.
    /// </remarks>
    public sealed class AiBlock : Capability
    {
        static readonly Being Running = Being.Flag(Reading.Of<IMyTerminalBlock>(block =>
        {
            var ai = block.Components.Get<MyAiBlockComponent>();
            return ai != null && ai.IsActivated;
        }));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyOffensiveCombatBlock || block is IMyDefensiveCombatBlock || block is IMyFlightMovementBlock
                || block is IMyPathRecorderBlock || block is IMyBasicMissionBlock;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The AI component says when it starts and stops, except where another block of its kind on the grid starting stops
        /// it; that is seen at the backstop look.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("ai-running", "AI running", Running).Announced()
                .Renaming("ActivateBehavior_On", false, "start the AI")
                .Renaming("ActivateBehavior_Off", false, "stop the AI")
                .Renaming("ActivateBehavior", false, "toggle the AI");
        }

        /// <summary>Told as the block's AI starts or stops, which its AI component says.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var ai = block.Components.Get<MyAiBlockComponent>();
            return ai == null ? null : new Listening(ai, told);
        }

        /// <summary>An AI component being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly MyAiBlockComponent _ai;
            readonly Action _told;

            public Listening(MyAiBlockComponent ai, Action told)
            {
                _ai = ai;
                _told = told;
                _ai.OnActivatedChanged += told;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _ai.OnActivatedChanged -= _told;
            }
        }
    }
}
