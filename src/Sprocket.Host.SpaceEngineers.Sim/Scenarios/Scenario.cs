using System.Collections.Generic;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>A build and its program, tested in the harness and written as a blueprint for the game.</summary>
public abstract class Scenario
{
    /// <summary>The id of the sign showing the checks.</summary>
    public const long SignId = 9900;

    /// <summary>The wide LCD the sign is.</summary>
    const string SignSubtype = "LargeLCDPanelWide";

    /// <summary>Where along the first floor's edge the sign stands, beside the programmable block.</summary>
    const int SignX = 1;

    /// <summary>The blueprint's name.</summary>
    public abstract string Name { get; }

    /// <summary>The checks only the game can answer, for the Testing artifact.</summary>
    public abstract IReadOnlyList<GameCheck> Checks { get; }

    /// <summary>Welds the build on <paramref name="rig" /> and lays it on <paramref name="deck" />.</summary>
    protected abstract void Lay(Rig rig, Deck deck);

    /// <summary>Writes the program on <paramref name="sim" /> through its editor.</summary>
    protected abstract void Program(Machine.Simulation sim);

    /// <summary>Builds it in the harness, ready to test or to write out.</summary>
    public Built Build(BlockDefinitions definitions)
    {
        var rig = new Rig();
        var deck = new Deck(definitions);
        Lay(rig, deck);
        deck.Sign(rig.TextPanel(SignId, "Test Instructions", SignSubtype), new Vector3I(SignX, 0, 0), Instructions.Of(Name, Checks));

        var sim = Machine.Simulation.On(rig);
        Program(sim);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Save();
        return new Built(sim, deck);
    }

    /// <summary>Adds a task to the open sequence that switches <paramref name="block" /> <paramref name="power" />, on or off.</summary>
    protected static void Power(Machine.Simulation sim, string block, string power)
    {
        var editor = sim.Editor;
        editor.PressAdd("task");
        editor.PressBlock(block);
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = power;
        editor.PressPrimary();
        sim.Tick();
    }

    /// <summary>The blueprint of what <paramref name="built" /> holds.</summary>
    public string Blueprint(Built built)
    {
        return Blueprints.Blueprint.Write(Name, built.Deck);
    }
}
