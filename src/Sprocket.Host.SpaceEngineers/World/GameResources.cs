using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The ids of the resources the game distributes, as its resource distributor names them.</summary>
    public static class GameResources
    {
        /// <summary>The game's resource id for electricity, transcribed from its resource distributor.</summary>
        public static readonly MyDefinitionId Electricity = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
    }
}
