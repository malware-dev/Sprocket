using Sprocket.Sim.Blueprints;

namespace Sprocket.Sim.Scenarios;

/// <summary>A scenario built in the harness.</summary>
public sealed class Built
{
    /// <summary>What <paramref name="sim" /> runs, laid on <paramref name="deck" />.</summary>
    public Built(Machine.Simulation sim, Deck deck)
    {
        Sim = sim;
        Deck = deck;
    }

    /// <summary>The rig with its program running.</summary>
    public Machine.Simulation Sim { get; private set; }

    /// <summary>Where each block stands.</summary>
    public Deck Deck { get; private set; }
}
