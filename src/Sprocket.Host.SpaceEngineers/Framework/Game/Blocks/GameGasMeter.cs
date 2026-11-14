using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>The gas a vent moves, as the game's resource components say it.</summary>
    /// <remarks>Reads what the resource distributor granted, not what the vent asked for.</remarks>
    public sealed class GameGasMeter : IGasMeter
    {
        static readonly MyDefinitionId Oxygen =
            new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), MyStringHash.GetOrCompute("Oxygen"));

        /// <inheritdoc />
        public float In(IMyAirVent vent)
        {
            var sink = vent.Components.Get<MyResourceSinkComponent>();
            return sink?.CurrentInputByType(Oxygen) ?? -1f;
        }

        /// <inheritdoc />
        public float Out(IMyAirVent vent)
        {
            var source = vent.Components.Get<MyResourceSourceComponent>();
            return source?.CurrentOutputByType(Oxygen) ?? -1f;
        }
    }
}
