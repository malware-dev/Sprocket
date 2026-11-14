using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Sprocket.Sim.Content;

namespace Sprocket.Sim.Items;

/// <summary>The game's items, read from its definitions and named in one of its languages, listed to the mod as the game lists them.</summary>
public sealed class ItemDefinitions : IItemCatalog
{
    const string Prefix = "MyObjectBuilder_";

    // A definition naming its text by a key the game's texts hold starts with this; any other name is shown as written.
    // From the game's definition loading.
    const string TextKey = "DisplayName_";

    // Each list of definitions the game makes items of, beside the name of one entry in it. From the game's content files:
    // physical items (ores, ingots, tools, bottles, food), components and ammunition.
    static readonly string[][] Kinds =
    {
        new[] { "PhysicalItems", "PhysicalItem" },
        new[] { "Components", "Component" },
        new[] { "AmmoMagazines", "AmmoMagazine" }
    };

    // Read once per language for the process: the content does not change while it runs, and reading it means reading
    // every definition file the game has.
    static readonly Dictionary<string, ItemDefinitions> Read = new Dictionary<string, ItemDefinitions>(StringComparer.Ordinal);

    readonly List<ItemDefinition> _items = new List<ItemDefinition>();
    readonly Dictionary<string, int> _at = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>The items <paramref name="documents" /> define, named through <paramref name="texts" /> where they name a key, and by their keys where it is null.</summary>
    public ItemDefinitions(IEnumerable<XDocument> documents, ITextSource? texts)
    {
        foreach (var document in documents)
        {
            foreach (var kind in Kinds)
            {
                var list = document.Root?.Element(kind[0]);
                if (list == null)
                    continue;

                foreach (var entry in list.Elements(kind[1]))
                    Add(entry, texts);
            }
        }
    }

    /// <summary>Every item, in the order the definitions give them.</summary>
    public IReadOnlyList<ItemDefinition> All => _items;

    /// <summary>The game's own items, named in <paramref name="culture" />, such as <c>no</c>, or in English where it is empty; none where neither the game nor the harness's cache of its content has them.</summary>
    public static ItemDefinitions FromGame(string culture = "")
    {
        if (Read.TryGetValue(culture, out var known))
            return known;

        var names = GameLanguage.In(culture);
        var items = new ItemDefinitions(names == null ? new List<XDocument>() : GameData.Holding("Data", "PhysicalItems", "Components", "AmmoMagazines"), names);
        Read[culture] = items;
        return items;
    }

    /// <summary>The item <paramref name="said" /> names, such as <c>Ingot/Iron</c>, or null where the game defines none.</summary>
    public ItemDefinition? Find(string said)
    {
        return _at.TryGetValue(said, out var at) ? _items[at] : null;
    }

    /// <inheritdoc />
    public void Public(List<string> names, List<string> ids)
    {
        foreach (var item in _items)
        {
            if (!item.IsPublic)
                continue;

            names.Add(item.Name);
            ids.Add(item.Said);
        }
    }

    void Add(XElement entry, ITextSource? texts)
    {
        var id = entry.Element("Id");
        if (id == null)
            return;

        var type = (string?)id.Element("TypeId") ?? (string?)id.Attribute("Type") ?? "";
        var subtype = (string?)id.Element("SubtypeId") ?? (string?)id.Attribute("Subtype") ?? "";
        if (type.StartsWith(Prefix, StringComparison.Ordinal))
            type = type.Substring(Prefix.Length);

        var said = type + "/" + subtype;
        var name = (string?)entry.Element("DisplayName") ?? "";
        if (name.StartsWith(TextKey, StringComparison.Ordinal))
            name = texts?.Find(name) ?? name;

        var item = new ItemDefinition(said, name, (string?)entry.Element("Public") != "false");
        if (_at.TryGetValue(said, out var at))
        {
            _items[at] = item;
            return;
        }

        _at[said] = _items.Count;
        _items.Add(item);
    }
}
