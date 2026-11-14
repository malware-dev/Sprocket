namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>What a selector's first column lists to pick from.</summary>
    public enum PickSource
    {
        /// <summary>The blocks on the construct.</summary>
        Blocks,

        /// <summary>The slots the logic system already has, picked by name.</summary>
        Slots,

        /// <summary>The logic system's states.</summary>
        States
    }
}
