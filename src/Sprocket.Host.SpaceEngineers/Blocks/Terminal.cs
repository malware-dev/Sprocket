using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>What every block in the terminal has: whether it works, its Custom Data, and its mark on the HUD.</summary>
    public sealed partial class Terminal : Capability
    {
        const string Unheard = "The game raises no event as a block stops being hacked.";

        // How long a block's Custom Data may be: the game cuts it there.
        const int DataLongest = 64000;

        static readonly Reading Works = Reading.Of<IMyTerminalBlock>(block => block.IsWorking);

        static readonly OwnValue Working = OwnValue.Flag(Works);

        static readonly OwnValue Hacked = OwnValue.Flag(Reading.Of<IMyTerminalBlock>(block => block.IsBeingHacked));

        static readonly OwnValue Data = OwnValue.Text<IMyTerminalBlock>(block => block.CustomData)
            .Writing<IMyTerminalBlock>((block, value) => block.CustomData = value.Text ?? "");

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return true;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("working", "working", Working).Status().In(Category.Power).Announced()
                .WithChange("starts_working", "starts working", "working", Literal.Of(true))
                .Reading("is working", "is not working").Announced()
                .WithChange("stops_working", "stops working", "working", Literal.Of(false))
                .Reading("is not working", "is working").Announced();

            kind.WithValue("being_hacked", "being hacked", Hacked).Looked(Unheard).In(Category.Access);

            kind.WithValue("custom_data", "custom data", Data).In(Category.Other).Announced().Lines(DataLongest);

            kind.Renaming("ShowOnHUD", false, "toggle show on HUD")
                .Renaming("ShowOnHUD", true, "show on HUD");
        }

        /// <summary>Told as the block starts or stops working and as its Custom Data changes.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return new Listening(block, told);
        }
    }
}
