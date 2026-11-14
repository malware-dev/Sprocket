using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System.Collections.Generic;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>
/// A programmable block holding more switched-off logic systems than one text panel lists at once, and a text panel beside it,
/// for pictures of the automation panel turning from one page of logic systems to the next.
/// </summary>
public sealed class StatusPages : Scenario
{
    /// <summary>The names of the logic systems, each started by a button panel named after it and switching Pages Light on.</summary>
    public static readonly IReadOnlyList<string> Systems = new[]
    {
        "Airlock", "Hangar Doors", "Cargo Lift", "Lights", "Refinery Feed", "Landing Gear", "Beacon", "Antenna", "Drills", "Welders"
    };

    /// <summary>The text panel the automation panel is shown on.</summary>
    public TextScreen Panel { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Status Pages";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("The automation panel turns its pages",
            "Sprocket Status Pages pasted, the automation panel set on Pages Panel.",
            new[] { "Watch Pages Panel as it turns from one page of logic systems to the next." },
            "Each page slides out to the left as the next slides in from the right, and settles.",
            "How motion looks at the game's frame rate is judged only on the game's own screen.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Pages Automation";
        Panel = rig.TextPanel(2, "Pages Panel");
        var fittings = new List<Fitting> { rig.Sprocket, Panel, rig.Light(3, "Pages Light"), rig.Battery(4, "Pages Battery") };
        for (var i = 0; i < Systems.Count; i++)
            fittings.Add(rig.Panel(10 + i, Systems[i] + " Button"));

        deck.Lay(fittings, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        for (var i = 0; i < Systems.Count; i++)
        {
            editor.GoToRoot();
            sim.Tick();
            editor.NewSystem(Systems[i]);
            editor.PressStart(SentenceText.WhenWord);
            editor.PressBlock(Systems[i] + " Button");
            editor.PressChoice("button 1 is pressed");
            editor.PressPrimary();
            sim.Tick();
            Power(sim, "Pages Light", "on");
        }

        // Left switched off: the automation panel lists each logic system that wants looking at, a switched-off one included.
        editor.GoToRoot();
        sim.Tick();
    }
}
