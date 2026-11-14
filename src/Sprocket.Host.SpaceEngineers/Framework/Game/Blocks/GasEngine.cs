using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A power producer that burns gas held in itself: the hydrogen engine, and any modded block built the same way.</summary>
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
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var definition = Definition(block);
            var fuel = definition.Fuel.FuelId.SubtypeName.ToLowerInvariant();
            var level = $"{fuel}_level";
            kind.WithValue(level, $"{fuel} level", Level(definition.FuelCapacity)).Percent()
                .Looked("An engine announces its fuel every tick it burns, which saves no look while it runs.").In(Category.Fuel)
                .WithChange($"runs_out_of_{fuel}", $"runs out of {fuel}", level, Literal.Of(0.0))
                .Reading($"has no {fuel}", $"has {fuel}").Announced();
        }

        /// <summary>Told as the engine runs out of fuel and as it gets some back.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return ResourceListener.Heard(block, told);
        }

        /// <summary>The fuel held, in percent of <paramref name="capacity" />, which is the definition's.</summary>
        static OwnValue Level(float capacity)
        {
            return OwnValue.Number<IMyPowerProducer>(engine =>
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
