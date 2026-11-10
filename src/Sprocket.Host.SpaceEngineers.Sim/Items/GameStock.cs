using System.Collections.Generic;
using Sprocket.Sim.Blueprints;

namespace Sprocket.Sim.Items;

/// <summary>The game's items, and which of them each kind of block's inventories take, from the game's definitions.</summary>
public sealed class GameStock
{
    readonly BlueprintClasses _classes;
    readonly BlockDefinitions _blocks;

    /// <summary>The items <paramref name="items" /> defines, taken by the blocks <paramref name="blocks" /> defines as the classes <paramref name="classes" /> say.</summary>
    public GameStock(ItemDefinitions items, BlueprintClasses classes, BlockDefinitions blocks)
    {
        Items = items;
        _classes = classes;
        _blocks = blocks;
    }

    /// <summary>The items, which the mod is given to list.</summary>
    public ItemDefinitions Items { get; }

    /// <summary>Whether the content held any items to read.</summary>
    public bool IsRead => Items.All.Count > 0;

    /// <summary>The game's own, with items named in <paramref name="culture" />, such as <c>no</c>, or in English where it is empty.</summary>
    /// <remarks>Empty where neither the game nor the harness's cache of its content has the definitions; see <see cref="IsRead" />.</remarks>
    public static GameStock FromGame(string culture = "")
    {
        return new GameStock(ItemDefinitions.FromGame(culture), BlueprintClasses.FromGame(), BlockDefinitions.FromGame());
    }

    /// <summary>
    /// The items the inventory at <paramref name="at" />, from 0, of a block of <paramref name="type" /> and <paramref name="subtype" />
    /// takes, as a step names them: a production block's input what its classes' blueprints take and its output what they give,
    /// and any other block's every item.
    /// </summary>
    /// <remarks>As the game builds a production block's inventories from its definition; a refinery passes over a class with a blueprint that makes its whole result at once.</remarks>
    public IReadOnlyList<string> Taken(string type, string subtype, int at)
    {
        var definition = _blocks.Find(type, subtype);
        if (definition == null || definition.Classes.Count == 0)
            return Every();

        var used = new List<string>();
        foreach (var name in definition.Classes)
        {
            if (type != "Refinery" || !_classes.IsAtomic(name))
                used.Add(name);
        }

        return at == 0 ? _classes.Taken(used) : _classes.Given(used);
    }

    List<string> Every()
    {
        var every = new List<string>();
        foreach (var item in Items.All)
            every.Add(item.Said);

        return every;
    }
}
