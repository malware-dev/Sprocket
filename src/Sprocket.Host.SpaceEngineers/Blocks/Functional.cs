using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block with a switch, which is every block that does anything.</summary>
    /// <remarks>
    /// Only a block with a switch gets the words for one. A cargo container has no on and off, and offering them would be
    /// offering a sentence that cannot come true.
    /// </remarks>
    public sealed class Functional : Capability
    {
        static readonly Reading On = Reading.Of<IMyFunctionalBlock>(block => block.Enabled);

        static readonly Reading Off = Reading.Of<IMyFunctionalBlock>(block => !block.Enabled);

        static readonly Doing SwitchOn = Doing.Of<IMyFunctionalBlock>(block => block.Enabled = true, On);

        static readonly Doing SwitchOff = Doing.Of<IMyFunctionalBlock>(block => block.Enabled = false, Off);

        static readonly Being SwitchedOn = Being.Flag(On);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyFunctionalBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("switched-on", "switched on", SwitchedOn).Announced()
                .WithChange("turns-on", "turns on", "switched-on", Literal.Of(true)).Reading("is on", "is off").Announced()
                .WithChange("turns-off", "turns off", "switched-on", Literal.Of(false)).Reading("is off", "is on").Announced()
                .WithAction(BlockKinds.SwitchOn, "switch on", SwitchOn, "OnOff_On")
                .WithAction(BlockKinds.SwitchOff, "switch off", SwitchOff, "OnOff_Off")
                .Renaming("OnOff", false, "toggle on/off");
        }

        /// <summary>Told as the block is switched on or off, which every functional block announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var functional = block as IMyFunctionalBlock;
            return functional == null ? null : new Listening(functional, told);
        }

        /// <summary>A switch being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyFunctionalBlock _block;
            readonly Action _told;

            public Listening(IMyFunctionalBlock block, Action told)
            {
                _block = block;
                _told = told;
                _block.EnabledChanged += Switched;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.EnabledChanged -= Switched;
            }

            void Switched(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
