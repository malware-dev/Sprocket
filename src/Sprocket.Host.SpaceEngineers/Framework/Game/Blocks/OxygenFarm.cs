using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>An oxygen farm, which makes oxygen from sunlight.</summary>
    public sealed partial class OxygenFarm : Capability
    {
        // The game gives the output as a fraction of the farm's best; every share Sprocket says is in percent.
        static readonly OwnValue Effectiveness = OwnValue.Real<IMyOxygenFarm>(farm => farm.GetOutput() * 100.0);

        static readonly OwnValue Able = OwnValue.Flag(Reading.Of<IMyOxygenFarm>(farm => farm.CanProduce));

        static readonly OwnValue Shadowed = OwnValue.Flag(Reading.Of<IMyOxygenFarm>(farm => farm.IsSolarOccluded));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyOxygenFarm;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("effectiveness", "effectiveness", Effectiveness).Percent().Announced().In(Category.Gas)
                .WithValue("able_to_produce", "able to produce", Able)
                .Looked("A farm announces nothing as its power or the world's oxygen comes and goes.").In(Category.Gas)
                .WithValue("in_shadow", "in shadow", Shadowed)
                .Looked("Shadow is worked out for the whole grid, which announces nothing as it falls.").In(Category.Gas);
        }

        /// <summary>Told as the farm's output changes, which it works out every hundred ticks.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return block is IMyOxygenFarm ? new Listening(block, told) : null;
        }
    }
}
