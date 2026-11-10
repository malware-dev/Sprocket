using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>A refinery joined to a cargo container by a wall of conveyor junctions, and three buttons moving items out of its input and its output.</summary>
public sealed class RefineryMoves : Scenario
{
    /// <summary>The refinery, whose input is its first inventory and its output its second.</summary>
    public Stockroom Refinery { get; private set; } = null!;

    /// <summary>The cargo container the conveyor wall joins to the refinery.</summary>
    public Stockroom Cargo { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Refinery Moves";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("A refinery and moved items",
            "Sprocket Refinery Moves pasted, its Refine logic system switched on. Iron ore in Move Refinery's input.",
            new[] { "Press Out Button, and watch for a minute.", "With ingots in the output, press Back Button.", "Press Store Button." },
            "Out Button moves the ore to Refinery Cargo; say whether the refinery pulls it back. Back Button moves nothing, since a refinery's input takes no ingots. Store Button moves the ingots to Refinery Cargo.",
            "What a refinery pulls and refuses is the game's own behavior, which the harness only imitates.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Refine Automation";
        Refinery = rig.Refinery(2, "Move Refinery");
        Cargo = rig.Cargo(3, "Refinery Cargo");
        deck.Lay(new List<Fitting>
        {
            Refinery, Cargo, rig.Sprocket, rig.Panel(4, "Out Button"), rig.Panel(5, "Store Button"), rig.Panel(6, "Back Button"),
            rig.Battery(7, "Refine Battery"), rig.Battery(8, "Refine Battery 2")
        }, Vector3I.Zero, 60);
        deck.ConveyorWall(Refinery, Cargo);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Refine");
        var moves = new[]
        {
            ("Out Button", "input", "Refinery Cargo", null),
            ("Store Button", "output", "Refinery Cargo", null),
            ("Back Button", "output", "Move Refinery", "input")
        };

        for (var i = 0; i < moves.Length; i++)
        {
            if (i > 0)
            {
                editor.GoToRoot();
                editor.Push(new LogicSystemPage(editor, editor.Program.Systems[0]));
                sim.Tick();
            }

            var (button, from, target, into) = moves[i];
            Moving(sim, button, from, target, into);
        }

        editor.GoToRoot();
        editor.SwitchSystem(editor.Program.Systems[0], true);
        sim.Tick();
    }

    /// <summary>
    /// A sequence started by the first button of <paramref name="button" /> that moves every item from the refinery's
    /// <paramref name="from" /> inventory to <paramref name="target" />, into its <paramref name="into" /> inventory where it has several.
    /// </summary>
    static void Moving(Machine.Simulation sim, string button, string from, string target, string? into)
    {
        var editor = sim.Editor;
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(button);
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();

        editor.PressAdd("task");
        editor.PressBlock("Move Refinery");
        sim.Tick();
        editor.Facets()!.Choose(from);
        sim.Tick();
        editor.PressChoice("move items");
        sim.Tick();
        ((SlotsField)editor.Pick().Arguments!.Form.Fields[0]).Select();
        sim.Tick();
        editor.PressBlock(target);
        sim.Tick();
        if (into != null)
        {
            editor.Facets()!.Choose(into);
            sim.Tick();
        }

        editor.PressPrimary();
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();
    }
}
