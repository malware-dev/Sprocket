using System;
using System.Collections.Generic;
using System.Linq;

namespace Sprocket.Sim.Scenarios;

/// <summary>Every scenario there is, found by its blueprint's name.</summary>
public static class Scenarios
{
    /// <summary>Each scenario, newly made.</summary>
    public static IReadOnlyList<Scenario> All => new Scenario[] { new SplitTwin(), new StockWatch(), new MoveItems(), new InventoryReadings() };

    /// <summary>The scenario whose blueprint is called <paramref name="name" />.</summary>
    public static Scenario Named(string name)
    {
        var found = All.FirstOrDefault(scenario => scenario.Name == name);
        if (found == null)
            throw new ArgumentException($"No scenario is called '{name}'. There are: {string.Join(", ", All.Select(scenario => scenario.Name))}.");

        return found;
    }
}
