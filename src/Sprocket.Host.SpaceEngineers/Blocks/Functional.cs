using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block with a switch, which is every block that does anything.</summary>
    /// <remarks>
    /// Only a block with a switch gets the words for one. A cargo container has no on and off, and offering them would be
    /// offering a sentence that cannot come true.
    /// <para>
    /// The switch is one value, set off, on, or flipped by the game's toggle.
    /// </para>
    /// </remarks>
    public sealed class Functional : Capability
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
                .WithChange("turns-on", "turns on", "power", Literal.Of(true)).Reading("is on", "is off").Announced()
                .WithChange("turns-off", "turns off", "power", Literal.Of(false)).Reading("is off", "is on").Announced()
                .Renaming("OnOff", false, "toggle on/off")
                .Flipping("power", "OnOff")
                .Hiding("OnOff_On", "OnOff_Off");
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
