namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>What a selector lists to pick from, chosen in the menu that opens before it.</summary>
    public enum PickSource
    {
        /// <summary>The blocks on the construct.</summary>
        Blocks,

        /// <summary>The slots the logic system already has, picked by name.</summary>
        Slots,

        /// <summary>The logic system's states.</summary>
        States,

        /// <summary>The mods the host has, each by the namespace it registered.</summary>
        Mods,

        /// <summary>An expression built on its own page, for a condition an if or a when tests.</summary>
        Expression
    }
}
