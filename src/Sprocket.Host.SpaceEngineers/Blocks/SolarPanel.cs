using System;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A solar panel, which takes no orders: read for the sunlight it catches and the power it gives.</summary>
    public sealed class SolarPanel : Capability
    {
        static readonly OwnValue Sunlight = OwnValue.Number<IMySolarPanel>((panel, seen) => Lit(panel, seen));

        static readonly OwnValue Output = OwnValue.Number<IMySolarPanel>(panel => panel.CurrentOutput);

        static readonly OwnValue MostOut = OwnValue.Number<IMySolarPanel>(panel => panel.MaxOutput);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySolarPanel;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("sunlight", "sunlight", Sunlight).Percent().Announced().In(Category.Power)
                .WithValue("output", "output", Output).Measured(" MW").Announced().In(Category.Power)
                .WithValue("maximum-output", "maximum output", MostOut).Measured(" MW").Announced().In(Category.Power);
        }

        /// <summary>Told as the panel's output changes.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var panel = block as IMySolarPanel;
            return panel == null ? null : new OutputListener(panel, told);
        }

        /// <summary>What <paramref name="panel" /> could make now against its best, from 0 to 100; none where nothing says its best.</summary>
        /// <remarks>Its best is looked up on the first read and kept in <paramref name="seen" />, so later reads cost one division.</remarks>
        static double Lit(IMySolarPanel panel, ReaderMemory seen)
        {
            if (!seen.Seen)
            {
                seen.Number = Best(panel);
                seen.Frame = 0;
            }

            return seen.Number > 0.0 ? panel.MaxOutput / seen.Number * 100.0 : 0.0;
        }

        /// <summary>The most <paramref name="panel" /> makes in full sun, in megawatts, or none where nothing has been loaded to say.</summary>
        static double Best(IMySolarPanel panel)
        {
            var definitions = MyDefinitionManager.Static;
            var definition = definitions?.GetCubeBlockDefinition(panel.BlockDefinition) as MyPowerProducerDefinition;

            return definition?.MaxPowerOutput ?? 0.0;
        }
    }
}
