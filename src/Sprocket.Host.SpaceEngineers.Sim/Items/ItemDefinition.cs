namespace Sprocket.Sim.Items;

/// <summary>What the game's definition of an item says: what it is, what it is called, and whether a player sees it.</summary>
public sealed class ItemDefinition
{
    /// <summary>The item <paramref name="said" />, shown as <paramref name="name" />, listed where <paramref name="isPublic" /> says.</summary>
    public ItemDefinition(string said, string name, bool isPublic)
    {
        Said = said;
        Name = name;
        IsPublic = isPublic;
    }

    /// <summary>The item as a step names it, its type and subtype: <c>Ingot/Iron</c>.</summary>
    public string Said { get; }

    /// <summary>The name the game shows it by, in the language it was read in.</summary>
    public string Name { get; }

    /// <summary>Whether the game lists it for players.</summary>
    public bool IsPublic { get; }
}
