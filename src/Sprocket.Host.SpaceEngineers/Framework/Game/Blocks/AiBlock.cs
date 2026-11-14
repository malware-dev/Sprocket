using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>An AI block, flight, task, combat or path, which starts and stops its AI and reads whether it runs.</summary>
    public sealed partial class AiBlock : Capability
    {
        static readonly OwnValue Running = OwnValue.Flag(Reading.Of<IMyTerminalBlock>(block =>
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
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("ai_running", "running", Running).Status().Announced()
                .Renaming("ActivateBehavior_On", false, "start the AI")
                .Renaming("ActivateBehavior_Off", false, "stop the AI")
                .Renaming("ActivateBehavior", false, "toggle the AI");
        }

        /// <summary>Told as the block's AI starts or stops.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var ai = block.Components.Get<MyAiBlockComponent>();
            return ai == null ? null : new Listening(ai, told);
        }
    }
}
