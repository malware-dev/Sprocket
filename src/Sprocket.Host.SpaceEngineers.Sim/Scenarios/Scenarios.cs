using System;
using System.Collections.Generic;
using System.Linq;

namespace Sprocket.Sim.Scenarios;

/// <summary>Every scenario there is, found by its blueprint's name.</summary>
public static class Scenarios
{
    /// <summary>Each scenario, newly made, and after them those kept in the Local folder of this machine.</summary>
    public static IReadOnlyList<Scenario> All => new Scenario[] { new SplitTwin(), new StockWatch(), new MoveItems(), new InventoryReadings(), new RefineryMoves(), new PistonStatus(), new ScriptLimit(), new ForcedDoor(), new OldAirlock(), new KeptCount(), new MoveAmounts(), new StatusPages() }.Concat(Local()).ToList();

    /// <summary>Every scenario kept in the Local folder of this machine, which git leaves out, newly made.</summary>
    static IEnumerable<Scenario> Local()
    {
        foreach (var type in typeof(Scenario).Assembly.GetTypes())
        {
            if (!type.IsAbstract && typeof(Scenario).IsAssignableFrom(type) && type.Namespace == typeof(Scenarios).Namespace + ".Local")
                yield return (Scenario)Activator.CreateInstance(type);
        }
    }

    /// <summary>The scenario whose blueprint is called <paramref name="name" />.</summary>
    public static Scenario Named(string name)
    {
        var found = All.FirstOrDefault(scenario => scenario.Name == name);
        if (found == null)
            throw new ArgumentException($"No scenario is called '{name}'. There are: {string.Join(", ", All.Select(scenario => scenario.Name))}.");

        return found;
    }
}
