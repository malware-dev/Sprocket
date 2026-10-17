using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>Two airlock decks joined by one armor block, which grinding splits.</summary>
public sealed class SplitTwin : Scenario
{
    /// <inheritdoc />
    public override string Name => "Sprocket Split Twin";

    /// <summary>The cell of the armor block joining the two decks.</summary>
    public Vector3I Bridge { get; private set; }

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("Each part keeps its own airlock after a split",
            "Sprocket Split Twin pasted, its Airlock logic system switched on.",
            new[]
            {
                "Press Fore Button, then Aft Button: each closes its inner door, opens its outer door, waits 3 seconds, closes it and opens the inner door again.",
                "Grind the one armor block joining the two decks.",
                "Open the editor from Fore Automation, then from Aft Automation.",
                "Press each button again."
            },
            "Each part has the Airlock logic system with only its own installation, nothing lost, and each button cycles its own doors.",
            "Only the game splits a grid; the harness can only detach a rotor head."),
        new GameCheck("The parts join again",
            "The split from the check before.",
            new[] { "Weld the joining armor block back.", "Open the editor, and press both buttons." },
            "One Airlock logic system with both installations, no Choose a version, and both buttons cycling their doors.",
            "Only the game merges grids, and its paste gives every block a new id.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Fore Automation";
        var fore = new List<Fitting> { rig.Sprocket, rig.Door(2, "Fore Inner Door", ""), rig.Door(3, "Fore Outer Door", ""), rig.Panel(4, "Fore Button"), rig.Battery(8, "Fore Battery") };
        var aft = new List<Fitting> { rig.Programmable(200, "Aft Automation"), rig.Door(5, "Aft Inner Door", ""), rig.Door(6, "Aft Outer Door", ""), rig.Panel(7, "Aft Button"), rig.Battery(9, "Aft Battery") };

        var far = deck.Lay(fore, Vector3I.Zero, 8);
        Bridge = new Vector3I(far.X + 1, 0, 0);
        deck.Floor(Bridge);
        deck.Lay(aft, Bridge + new Vector3I(1, 0, 0), 8);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Airlock");
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock("Fore Button");
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();

        // A whole cycle, ending as it starts, so every press moves both doors.
        Task(editor, "Fore Inner Door", "close");
        Task(editor, "Fore Outer Door", "open");
        editor.PressAdd("delay");
        for (var i = 0; i < 3; i++)
            sim.Tick();
        var length = ((SequencePage)editor.Current).Lengths!.Box;
        length.Text = "3";
        length.Submit!.Execute();
        sim.Tick();
        Task(editor, "Fore Outer Door", "close");
        Task(editor, "Fore Inner Door", "open");
        sim.Tick();

        // Filled on its panel, so its button is wired as a person's is.
        var system = editor.Program.Systems[0];
        system.First!.Name = "Fore";
        var aft = system.AddInstance();
        aft.Name = "Aft";
        editor.Choose(system, aft);
        editor.Push(new BlueprintPage(editor, system));
        sim.Tick();
        foreach (var row in editor.Parts())
        {
            row.Fills!.Execute();
            sim.Tick();
            editor.PressBlock(row.Name.Replace("Fore", "Aft"));
            if (row.Name.EndsWith("Button"))
                editor.PressChoice("button 1 is pressed");

            editor.PressPrimary();
            sim.Tick();
        }

        editor.GoToRoot();
        editor.SwitchSystem(system, true);
        sim.Tick();
    }

    /// <summary>Adds a step doing <paramref name="action" /> on <paramref name="block" />.</summary>
    static void Task(SprocketEditor editor, string block, string action)
    {
        editor.PressAdd("task");
        editor.PressBlock(block);
        editor.PressChoice(action);
        editor.PressPrimary();
    }
}
