using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Sprocket.Sim.Content;

namespace Sprocket.Sim.Items;

/// <summary>The game's blueprint classes, read from its definitions, each with the items its blueprints take and give.</summary>
/// <remarks>A production block's input takes what the blueprints of its classes take, and its output what they give, as the game builds a production block's inventories.</remarks>
public sealed class BlueprintClasses
{
    const string Prefix = "MyObjectBuilder_";

    // Read once for the process: the content does not change while it runs, and reading it means reading every
    // definition file the game has.
    static BlueprintClasses? _read;

    readonly Dictionary<string, Recipe> _recipes = new Dictionary<string, Recipe>(StringComparer.Ordinal);
    readonly Dictionary<string, List<string>> _classes = new Dictionary<string, List<string>>(StringComparer.Ordinal);

    /// <summary>The blueprints and classes <paramref name="documents" /> define.</summary>
    public BlueprintClasses(IEnumerable<XDocument> documents)
    {
        foreach (var document in documents)
        {
            foreach (var blueprint in document.Root?.Element("Blueprints")?.Elements("Blueprint") ?? new XElement[0])
                AddRecipe(blueprint);

            foreach (var entry in document.Root?.Element("BlueprintClassEntries")?.Elements("Entry") ?? new XElement[0])
            {
                var name = (string?)entry.Attribute("Class");
                var blueprint = (string?)entry.Attribute("BlueprintSubtypeId");
                if (name == null || blueprint == null)
                    continue;

                if (!_classes.TryGetValue(name, out var listed))
                    _classes[name] = listed = new List<string>();

                listed.Add(blueprint);
            }
        }
    }

    /// <summary>The game's own blueprint classes; none where neither the game nor the harness's cache of its content has them.</summary>
    public static BlueprintClasses FromGame()
    {
        return _read ??= new BlueprintClasses(GameData.Holding("Data", "Blueprints", "BlueprintClassEntries"));
    }

    /// <summary>Whether the class <paramref name="name" /> holds any blueprint.</summary>
    public bool Has(string name) => _classes.ContainsKey(name);

    /// <summary>Whether a blueprint of the class <paramref name="name" /> makes its whole result at once, which keeps a refinery from using the class.</summary>
    public bool IsAtomic(string name)
    {
        foreach (var recipe in Recipes(name))
        {
            if (recipe.Atomic)
                return true;
        }

        return false;
    }

    /// <summary>Every item a blueprint of one of <paramref name="classes" /> takes, each once, as a step names it.</summary>
    public IReadOnlyList<string> Taken(IEnumerable<string> classes) => Items(classes, true);

    /// <summary>Every item a blueprint of one of <paramref name="classes" /> gives, each once, as a step names it.</summary>
    public IReadOnlyList<string> Given(IEnumerable<string> classes) => Items(classes, false);

    List<string> Items(IEnumerable<string> classes, bool taken)
    {
        var items = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in classes)
        {
            foreach (var recipe in Recipes(name))
            {
                foreach (var item in taken ? recipe.Takes : recipe.Gives)
                {
                    if (seen.Add(item))
                        items.Add(item);
                }
            }
        }

        return items;
    }

    IEnumerable<Recipe> Recipes(string name)
    {
        if (!_classes.TryGetValue(name, out var listed))
            yield break;

        foreach (var blueprint in listed)
        {
            if (_recipes.TryGetValue(blueprint, out var recipe))
                yield return recipe;
        }
    }

    void AddRecipe(XElement blueprint)
    {
        var id = blueprint.Element("Id");
        var subtype = (string?)id?.Element("SubtypeId") ?? (string?)id?.Attribute("Subtype");
        if (subtype == null)
            return;

        var recipe = new Recipe { Atomic = (string?)blueprint.Element("Atomic") == "true" };
        foreach (var item in blueprint.Element("Prerequisites")?.Elements("Item") ?? new XElement[0])
            recipe.Takes.Add(Said(item));

        // A blueprint gives one item as a Result of its own, or several under Results.
        var result = blueprint.Element("Result");
        if (result != null)
            recipe.Gives.Add(Said(result));

        foreach (var item in blueprint.Element("Results")?.Elements("Item") ?? new XElement[0])
            recipe.Gives.Add(Said(item));

        _recipes[subtype] = recipe;
    }

    static string Said(XElement item)
    {
        var type = (string?)item.Attribute("TypeId") ?? "";
        if (type.StartsWith(Prefix, StringComparison.Ordinal))
            type = type.Substring(Prefix.Length);

        return type + "/" + ((string?)item.Attribute("SubtypeId") ?? "");
    }

    /// <summary>What one blueprint takes and gives.</summary>
    sealed class Recipe
    {
        /// <summary>The items it takes, as a step names them.</summary>
        public readonly List<string> Takes = new List<string>();

        /// <summary>The items it gives, as a step names them.</summary>
        public readonly List<string> Gives = new List<string>();

        /// <summary>Whether it makes its whole result at once.</summary>
        public bool Atomic;
    }
}
