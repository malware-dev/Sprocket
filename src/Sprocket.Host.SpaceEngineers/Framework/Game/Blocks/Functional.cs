using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A block with a switch, which is every block that does anything.</summary>
    public sealed partial class Functional : Capability
    {
        static readonly Reading On = Reading.Of<IMyFunctionalBlock>(block => block.Enabled);

        static readonly OwnValue SwitchedOn = OwnValue.Flag(On)
            .Writing<IMyFunctionalBlock>((block, value) => block.Enabled = value.Flag);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyFunctionalBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("power", "power", SwitchedOn).In(Category.Power).Announced()
                .WithChange("turns_on", "turns on", "power", Literal.Of(true)).Reading("is on", "is off").Announced()
                .WithChange("turns_off", "turns off", "power", Literal.Of(false)).Reading("is off", "is on").Announced()
                .Renaming("OnOff", false, "toggle on/off")
                .Flipping("power", "OnOff")
                .Hiding("OnOff_On", "OnOff_Off");
        }

        /// <summary>Told as the block is switched on or off.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var functional = block as IMyFunctionalBlock;
            return functional == null ? null : new Listening(functional, told);
        }
    }
}
