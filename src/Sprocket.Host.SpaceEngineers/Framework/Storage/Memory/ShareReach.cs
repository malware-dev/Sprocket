namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>Who a shared blueprint is for.</summary>
    /// <remarks>Stored by number in the world's memory: a number is never changed or given to another value.</remarks>
    public enum ShareReach
    {
        /// <summary>Everyone in the world, whenever they join.</summary>
        Everyone = 0,

        /// <summary>The sharer's faction, as it is when somebody looks.</summary>
        Faction = 1,

        /// <summary>The players the sharer named.</summary>
        Players = 2
    }
}
