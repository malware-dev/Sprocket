using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A wind turbine, which takes no orders: read for the wind it catches and the power it gives.</summary>
    /// <remarks>The game's three shares of wind, 0 to 1, are read as percentages.</remarks>
    public sealed class WindTurbine : Capability
    {
        static readonly OwnValue Wind = OwnValue.Number<IMyWindTurbine>(turbine => turbine.Effectivity * 100.0);

        static readonly OwnValue Strength = OwnValue.Number<IMyWindTurbine>(turbine => turbine.WindEffectivity * 100.0);

        static readonly OwnValue Clearance = OwnValue.Number<IMyWindTurbine>(turbine => turbine.PlacementEffectivity * 100.0);

        static readonly OwnValue Output = OwnValue.Number<IMyWindTurbine>(turbine => turbine.CurrentOutput);

        static readonly OwnValue MostOut = OwnValue.Number<IMyWindTurbine>(turbine => turbine.MaxOutput);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyWindTurbine;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("wind", "wind", Wind).Percent().Announced().In(Category.Power)
                .WithValue("wind_strength", "wind strength", Strength).Percent().Announced().In(Category.Power)
                .WithValue("clearance", "clearance", Clearance).Percent().Announced().In(Category.Power)
                .WithValue("output", "output", Output).Measured(" MW").Announced().In(Category.Power)
                .WithValue("maximum_output", "maximum output", MostOut).Measured(" MW").Announced().In(Category.Power);
        }

        /// <summary>Told as the turbine's output changes.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var turbine = block as IMyWindTurbine;
            return turbine == null ? null : new OutputListener(turbine, told);
        }
    }
}
