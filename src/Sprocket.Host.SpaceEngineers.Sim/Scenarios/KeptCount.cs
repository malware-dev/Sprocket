using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System.Collections.Generic;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>
/// A programmable block holding two logic systems, one kept to that block and one listed everywhere, and a text panel beside
/// it, for a picture of the automation panel on each: the block's own screen counts both, and the panel only the one listed
/// everywhere.
/// </summary>
public sealed class KeptCount : Scenario
{
    /// <summary>The text panel the automation panel is shown on beside the block's own screen.</summary>
    public TextScreen Panel { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Kept Count";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("A kept system counts only on its own block's screen",
            "Sprocket Kept Count pasted, the automation panel set on Kept Automation's screen and on Kept Panel.",
            new[] { "Read the count on each." },
            "Kept Automation's screen counts two logic systems, and Kept Panel counts one.",
            "Only the game shows a real LCD on a real block.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Kept Automation";
        Panel = rig.TextPanel(2, "Kept Panel");
        deck.Lay(new List<Fitting> { rig.Sprocket, Panel, rig.Panel(3, "Kept Button"), rig.Panel(6, "Everywhere Button"), rig.Light(4, "Kept Light"), rig.Battery(5, "Kept Battery") },
            Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        Light(sim, "Kept Here", "Kept Button", "on");
        Light(sim, "Listed Everywhere", "Everywhere Button", "off");

        // Kept to the block the editor was opened from, by the action the logic system's page offers in developer mode.
        var kept = editor.Program.Systems[0];
        editor.DeveloperMode = true;
        editor.OpenedFrom = sim.Machine.Sprocket.Id;
        editor.Push(new LogicSystemPage(editor, kept));
        sim.Tick();
        editor.PressAction(editor.Texts.SystemsKeepHere);
        sim.Tick();

        editor.GoToRoot();
        foreach (var system in editor.Program.Systems)
            editor.SwitchSystem(system, true);
        sim.Tick();
    }

    /// <summary>A logic system called <paramref name="name" /> whose <paramref name="button" /> sets Kept Light <paramref name="power" />.</summary>
    static void Light(Machine.Simulation sim, string name, string button, string power)
    {
        var editor = sim.Editor;
        editor.GoToRoot();
        sim.Tick();
        editor.NewSystem(name);
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(button);
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();
        Power(sim, "Kept Light", power);
        editor.GoToRoot();
        sim.Tick();
    }
}
