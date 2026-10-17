using System.Collections.Generic;

namespace Sprocket.Sim.Scenarios;

/// <summary>One check only the game can answer.</summary>
public sealed class GameCheck
{
    /// <summary>A check called <paramref name="title" />.</summary>
    public GameCheck(string title, string setup, IReadOnlyList<string> steps, string expect, string why)
    {
        Title = title;
        Setup = setup;
        Steps = steps;
        Expect = expect;
        Why = why;
    }

    /// <summary>What it checks.</summary>
    public string Title { get; private set; }

    /// <summary>What to have in the world before the steps.</summary>
    public string Setup { get; private set; }

    /// <summary>What to do, in order.</summary>
    public IReadOnlyList<string> Steps { get; private set; }

    /// <summary>What the game shows when it passes.</summary>
    public string Expect { get; private set; }

    /// <summary>What the harness cannot reach.</summary>
    public string Why { get; private set; }
}
