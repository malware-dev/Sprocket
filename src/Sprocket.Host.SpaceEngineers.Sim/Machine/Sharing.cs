namespace Sprocket.Sim.Machine;

/// <summary>Who a block's owner shares it with, as the game's own share setting has it.</summary>
public enum Sharing
{
    /// <summary>Its owner alone.</summary>
    Nobody,

    /// <summary>Its owner's faction.</summary>
    Faction,

    /// <summary>Everyone.</summary>
    All
}
