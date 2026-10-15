using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A wind turbine, which takes no orders: read for the wind it catches and the power it gives.</summary>
    /// <remarks>
    /// The game's three shares of wind are 0 to 1, and read here as percentages: the wind where the turbine stands, how
    /// clear of blocks and ground it is, and the two together. A turbine makes only what the grid draws, so its output
    /// follows the load as much as the wind.
    /// </remarks>
    public sealed class WindTurbine : Capability
    {
        static readonly Being Wind = Being.Number<IMyWindTurbine>(turbine => turbine.Effectivity * 100.0);

        static readonly Being Strength = Being.Number<IMyWindTurbine>(turbine => turbine.WindEffectivity * 100.0);

        static readonly Being Clearance = Being.Number<IMyWindTurbine>(turbine => turbine.PlacementEffectivity * 100.0);

        static readonly Being Output = Being.Number<IMyWindTurbine>(turbine => turbine.CurrentOutput);

        static readonly Being MostOut = Being.Number<IMyWindTurbine>(turbine => turbine.MaxOutput);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyWindTurbine;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("wind", "wind", Wind).Percent().Announced().In(Category.Power)
                .WithValue("wind-strength", "wind strength", Strength).Percent().Announced().In(Category.Power)
                .WithValue("clearance", "clearance", Clearance).Percent().Announced().In(Category.Power)
                .WithValue("output", "output", Output).Measured(" MW").Announced().In(Category.Power)
                .WithValue("maximum-output", "maximum output", MostOut).Measured(" MW").Announced().In(Category.Power);
        }

        /// <summary>Told as the turbine's output changes, which it announces as the wind or its clearance moves what it can make.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var turbine = block as IMyWindTurbine;
            return turbine == null ? null : new Producing(turbine, told);
        }
    }
}
