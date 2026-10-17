using System;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A power producer that burns gas held in itself: the hydrogen engine, and any modded block built the same way.</summary>
    /// <remarks>
    /// The engine keeps its fuel where no interface shows it. Its resource source holds the fuel as the electricity it can
    /// still give, and the definition says how much it holds when full. What the engine makes and how hard it works is
    /// every power producer's, and not this.
    /// </remarks>
    public sealed class GasEngine : Capability
    {
        static readonly MyDefinitionId Electricity =
            new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyPowerProducer && Definition(block) != null;
        }

        /// <inheritdoc />
        /// <remarks>The fuel is named by the definition, hydrogen in every engine the game has.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var definition = Definition(block);
            var fuel = definition.Fuel.FuelId.SubtypeName.ToLowerInvariant();
            var level = $"{fuel}-level";
            kind.WithValue(level, $"{fuel} level", Level(definition.FuelCapacity)).Percent()
                .Looked("An engine announces its fuel every tick it burns, which saves no look while it runs.").In(Category.Fuel)
                .WithChange($"runs-out-of-{fuel}", $"runs out of {fuel}", level, Literal.Of(0.0))
                .Reading($"has no {fuel}", $"has {fuel}").Announced();
        }

        /// <summary>Told as the engine runs out of fuel and as it gets some back, which its resource source raises.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return ResourceListener.Heard(block, told);
        }

        /// <summary>The fuel held, in percent of <paramref name="capacity" />, which is the definition's.</summary>
        /// <remarks>One per kind, since the capacity is the definition's and read once.</remarks>
        static Being Level(float capacity)
        {
            return Being.Number<IMyPowerProducer>(engine =>
            {
                var source = engine.Components.Get<MyResourceSourceComponent>();
                return source == null || capacity <= 0f ? 0.0 : source.RemainingCapacityByType(Electricity) / capacity * 100.0;
            });
        }

        /// <summary>What the game says this block is, where it is one that burns gas it holds, or null.</summary>
        static MyGasFueledPowerProducerDefinition Definition(IMyTerminalBlock block)
        {
            var definitions = MyDefinitionManager.Static;
            return definitions?.GetCubeBlockDefinition(block.BlockDefinition) as MyGasFueledPowerProducerDefinition;
        }
    }
}
