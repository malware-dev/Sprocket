using System;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A solar panel, which takes no orders: read for the sunlight it catches and the power it gives.</summary>
    /// <remarks>
    /// A panel makes only what the grid draws, so its output follows the load as much as the sun; sunlight is what it could
    /// make against its best, which is the reading for the sun.
    /// </remarks>
    public sealed class SolarPanel : Capability
    {
        static readonly Being Sunlight = Being.Number<IMySolarPanel>((panel, seen) => Lit(panel, seen));

        static readonly Being Output = Being.Number<IMySolarPanel>(panel => panel.CurrentOutput);

        static readonly Being MostOut = Being.Number<IMySolarPanel>(panel => panel.MaxOutput);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySolarPanel;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("sunlight", "sunlight", Sunlight).Announced().In(Category.Power)
                .WithValue("output", "output", Output).Announced().In(Category.Power)
                .WithValue("maximum-output", "maximum output", MostOut).Announced().In(Category.Power);
        }

        /// <summary>Told as the panel's output changes, which it announces as the light it catches moves what it can make.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var panel = block as IMySolarPanel;
            return panel == null ? null : new Producing(panel, told);
        }

        /// <summary>What <paramref name="panel" /> could make now against its best, from 0 to 100; none where nothing says its best.</summary>
        /// <remarks>
        /// Its best is the definition's, looked up on the first read and kept in <paramref name="seen" /> from then on, so a
        /// read after that is one division.
        /// </remarks>
        static double Lit(IMySolarPanel panel, Recall seen)
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
            var definition = definitions == null
                ? null
                : definitions.GetCubeBlockDefinition(panel.BlockDefinition) as MyPowerProducerDefinition;

            return definition == null ? 0.0 : definition.MaxPowerOutput;
        }
    }
}
