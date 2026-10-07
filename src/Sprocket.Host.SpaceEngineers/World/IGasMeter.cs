using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>How much gas a vent is moving, which only the game can say.</summary>
    /// <remarks>
    /// The game says it through its own concrete resource components, which nothing but the game can answer. Behind this,
    /// the harness answers it over the vents it makes up, so a room that has nowhere to put its air can be tested at all.
    /// </remarks>
    public interface IGasMeter
    {
        /// <summary>What <paramref name="vent" /> is being given, which is how it fills a room; below nought where nothing says.</summary>
        float In(IMyAirVent vent);

        /// <summary>What <paramref name="vent" /> is passing on, which is how it empties a room; below nought where nothing says.</summary>
        float Out(IMyAirVent vent);
    }
}
