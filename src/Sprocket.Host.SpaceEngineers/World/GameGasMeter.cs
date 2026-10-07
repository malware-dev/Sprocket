using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The gas a vent moves, as the game's resource components say it.</summary>
    /// <remarks>
    /// What the resource distributor granted rather than what the vent asked for, which is why a construct with no oxygen
    /// reads as a vent moving nothing rather than one working for ever.
    /// </remarks>
    public sealed class GameGasMeter : IGasMeter
    {
        static readonly MyDefinitionId Oxygen =
            new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), MyStringHash.GetOrCompute("Oxygen"));

        /// <inheritdoc />
        public float In(IMyAirVent vent)
        {
            var sink = vent.Components.Get<MyResourceSinkComponent>();
            return sink == null ? -1f : sink.CurrentInputByType(Oxygen);
        }

        /// <inheritdoc />
        public float Out(IMyAirVent vent)
        {
            var source = vent.Components.Get<MyResourceSourceComponent>();
            return source == null ? -1f : source.CurrentOutputByType(Oxygen);
        }
    }
}
