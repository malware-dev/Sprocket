using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>How much gas a vent is moving, which only the game can say.</summary>
    public interface IGasMeter
    {
        /// <summary>What <paramref name="vent" /> is being given, or below 0 where nothing says.</summary>
        float In(IMyAirVent vent);

        /// <summary>What <paramref name="vent" /> is passing on, or below 0 where nothing says.</summary>
        float Out(IMyAirVent vent);
    }
}
