using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An oxygen farm, which makes oxygen from sunlight.</summary>
    public sealed class Farm : Capability
    {
        // The game gives the output as a fraction of the farm's best; every share Sprocket says is in percent.
        static readonly Being Effectiveness = Being.Number<IMyOxygenFarm>(farm => farm.GetOutput() * 100.0);

        static readonly Being Able = Being.Flag(Reading.Of<IMyOxygenFarm>(farm => farm.CanProduce));

        static readonly Being Shadowed = Being.Flag(Reading.Of<IMyOxygenFarm>(farm => farm.IsSolarOccluded));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyOxygenFarm;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("effectiveness", "effectiveness", Effectiveness).Announced().In(Category.Gas)
                .WithValue("able-to-produce", "able to produce", Able)
                .Looked("A farm announces nothing as its power or the world's oxygen comes and goes.").In(Category.Gas)
                .WithValue("in-shadow", "in shadow", Shadowed)
                .Looked("Shadow is worked out for the whole grid, which announces nothing as it falls.").In(Category.Gas);
        }

        /// <summary>Told as the farm's output changes, which it works out every hundred ticks and announces when it moves.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return block is IMyOxygenFarm ? new Listening(block, told) : null;
        }

        /// <summary>A farm being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyTerminalBlock _farm;
            readonly Action _told;

            public Listening(IMyTerminalBlock farm, Action told)
            {
                _farm = farm;
                _told = told;
                _farm.PropertiesChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _farm.PropertiesChanged -= Changed;
            }

            void Changed(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
