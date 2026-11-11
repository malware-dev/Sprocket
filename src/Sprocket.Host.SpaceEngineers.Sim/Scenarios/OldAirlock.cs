using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>
/// A door, a piston, a button whose sequence closes the door, moves the piston and opens the door again, and a trigger that
/// lights a lamp as the door finishes opening: members whose ids an earlier draft spelled with hyphens, for a test of the
/// upgrade from that draft.
/// </summary>
public sealed class OldAirlock : Scenario
{
    /// <summary>The door the sequence closes and opens.</summary>
    public Door Door { get; private set; } = null!;

    /// <summary>The piston the sequence moves.</summary>
    public Piston Piston { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Old Airlock";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("An old program comes through the upgrade",
            "Sprocket Old Airlock pasted, with its program in an earlier draft, Old Door closed.",
            new[] { "Press Cycle Button." },
            "Old Door closes, Old Piston moves to 2 m, Old Door opens, and Cycled and Opened come on.",
            "The harness reads the old text; only the game loads it out of a real block's storage.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Old Automation";
        Door = rig.Door(2, "Old Door", "");
        Piston = rig.Piston(3, "Old Piston");
        deck.Lay(new List<Fitting>
        {
            Door, Piston, rig.Sprocket, rig.Panel(4, "Cycle Button"), rig.Light(5, "Cycled"), rig.Light(6, "Opened"),
            rig.Battery(7, "Old Battery")
        }, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Airlock");
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock("Cycle Button");
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();

        Power(sim, "Cycled", "off");
        TellDoor(sim, "close");
        editor.PressAdd("task");
        editor.PressBlock("Old Piston");
        sim.Tick();
        editor.PressChoice("move to");
        sim.Tick();
        var fields = editor.Pick().Arguments!.Form.Fields;
        fields[0].Text = "2";
        fields[1].Text = "2";
        editor.PressPrimary();
        sim.Tick();
        TellDoor(sim, "open");
        Power(sim, "Cycled", "on");
        editor.GoToRoot();
        sim.Tick();

        var system = editor.Program.Systems[0];
        editor.Push(new LogicSystemPage(editor, system));
        sim.Tick();
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock("Old Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        sim.Tick();
        Power(sim, "Opened", "on");

        editor.GoToRoot();
        editor.SwitchSystem(system, true);
        sim.Tick();
    }

    /// <summary>A task that tells Old Door to <paramref name="action" />.</summary>
    static void TellDoor(Machine.Simulation sim, string action)
    {
        var editor = sim.Editor;
        editor.PressAdd("task");
        editor.PressBlock("Old Door");
        sim.Tick();
        editor.PressChoice(action);
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();
    }
}
