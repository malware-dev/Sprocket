using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>A piston, a button that moves it to 3 m at 2 m/s, and a light the move switches on once the piston gets there.</summary>
public sealed class PistonStatus : Scenario
{
    /// <summary>The piston the button moves.</summary>
    public Piston Piston { get; private set; } = null!;

    /// <summary>The light the move switches on when it is done.</summary>
    public Lamp Arrived { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Piston Status";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("A piston's status at its limit",
            "Sprocket Piston Status pasted, with a head added to Status Piston, and its Piston logic system switched on.",
            new[] { "Extend Status Piston fully and read its status.", "Set its speed to 0 at the end and read it again.", "Press Three Button, and read the status once At Three comes on." },
            "extended, then stopped, then extended at 3 m.",
            "The harness moves no real piston, so only the game says what status a piston reports at its limit.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Piston Automation";
        Piston = rig.Piston(2, "Status Piston");
        Arrived = rig.Light(3, "At Three");
        deck.Lay(new List<Fitting> { Piston, rig.Sprocket, rig.Panel(4, "Three Button"), Arrived, rig.Battery(5, "Piston Battery") }, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Piston");
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock("Three Button");
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();

        Power(sim, "At Three", "off");
        editor.PressAdd("task");
        editor.PressBlock("Status Piston");
        sim.Tick();
        editor.PressChoice("move to");
        sim.Tick();
        var fields = editor.Pick().Arguments!.Form.Fields;
        fields[0].Text = "3";
        fields[1].Text = "2";
        editor.PressPrimary();
        sim.Tick();
        Power(sim, "At Three", "on");

        editor.GoToRoot();
        editor.SwitchSystem(editor.Program.Systems[0], true);
        sim.Tick();
    }
}
