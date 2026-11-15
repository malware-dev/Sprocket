using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Players
{
    /// <summary>How the player's character stands, read from the game as it is asked.</summary>
    public sealed class CharacterVitals : IVitals
    {
        // The character stats that hold health and food (the game's Content/Data/EntityStats.sbc).
        static readonly MyStringHash HealthStat = MyStringHash.GetOrCompute("Health");
        static readonly MyStringHash FoodStat = MyStringHash.GetOrCompute("Food");

        // The gas a suit breathes (the game's Content/Data/GasProperties.sbc).
        static readonly MyDefinitionId OxygenGas = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");

        /// <inheritdoc />
        public bool Present => Character != null;

        /// <inheritdoc />
        public bool HasOxygen => MyAPIGateway.Session.SessionSettings.EnableOxygen;

        /// <inheritdoc />
        public bool HasFood => MyAPIGateway.Session.SessionSettings.FoodConsumptionRate > 0f;

        /// <inheritdoc />
        public float Health => Share(HealthStat);

        /// <inheritdoc />
        public float Energy => Character?.SuitEnergyLevel ?? 0f;

        /// <inheritdoc />
        public float Oxygen => Character?.GetSuitGasFillLevel(OxygenGas) ?? 0f;

        /// <inheritdoc />
        public float Food => Share(FoodStat);

        static IMyCharacter Character => MyAPIGateway.Session?.Player?.Character;

        /// <summary>The stat <paramref name="id" /> of the character as a share of its most, or 0 where it has none.</summary>
        static float Share(MyStringHash id)
        {
            MyEntityStat stat = null;
            // A character's stats are filed under MyCharacterStatComponent, which Get matches exactly.
            Character?.Components.Get<MyCharacterStatComponent>()?.TryGetStat(id, out stat);
            return stat == null || stat.MaxValue <= 0f ? 0f : stat.Value / stat.MaxValue;
        }
    }
}
