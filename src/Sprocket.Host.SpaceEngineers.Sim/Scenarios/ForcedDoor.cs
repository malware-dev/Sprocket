using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>A door and a button whose sequence opens it forced, with nothing after.</summary>
public sealed class ForcedDoor : Scenario
{
    /// <summary>The door the button opens.</summary>
    public Door Door { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Forced Door";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("A forced door against a player",
            "Sprocket Forced Door pasted, its Forced logic system switched on, Forced Door closed.",
            new[] { "Press Force Button, and close the door from its terminal as it opens.", "Press Force Button again, and switch the door off while it is forced." },
            "The door keeps opening each time it is reversed, arrives and is released. Switching it off releases it at once.",
            "The harness moves no real door and has no player pressing buttons.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Forced Automation";
        Door = rig.Door(2, "Forced Door", "");
        deck.Lay(new List<Fitting> { Door, rig.Sprocket, rig.Panel(3, "Force Button"), rig.Battery(4, "Forced Battery") }, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Forced");
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock("Force Button");
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();

        editor.PressAdd("task");
        editor.PressBlock("Forced Door");
        sim.Tick();
        editor.PressChoice("open");
        sim.Tick();
        editor.Pick().Arguments!.Form.Fields[0].Text = "forced";
        editor.PressPrimary();
        sim.Tick();

        editor.GoToRoot();
        editor.SwitchSystem(editor.Program.Systems[0], true);
        sim.Tick();
    }
}
