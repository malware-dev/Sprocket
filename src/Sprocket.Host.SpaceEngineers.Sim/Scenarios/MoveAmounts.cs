using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>
/// Two cargo containers joined by a conveyor junction and four buttons moving from the first to the second: 100 kg of iron ingots,
/// 2.5 steel plates, every item, and 100 kg of iron ingots that moves on when there is nothing to move, switching a light on after.
/// </summary>
public sealed class MoveAmounts : Scenario
{
    /// <summary>The container the items start in.</summary>
    public Stockroom Source { get; private set; } = null!;

    /// <summary>The container the items go to.</summary>
    public Stockroom Target { get; private set; } = null!;

    /// <summary>The light the move that moves on switches on after it.</summary>
    public Lamp MovedOn { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Move Amounts";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("Moved items arrive in the real amounts",
            "Sprocket Move Amounts pasted, its Amounts logic system switched on. Iron ingots, stone and steel plates in Amounts A.",
            new[]
            {
                "Press Ingots Button.", "Press Plates Button.", "Press All Button.",
                "With Amounts A empty and Moved On switched off, press Empty Button."
            },
            "Ingots moves 100 kg of iron ingots exactly. Plates moves 2 steel plates, the whole ones in 2.5. All empties Amounts A. Empty moves nothing and switches Moved On on.",
            "The harness models no conveyors, and only the game's inventories say how much of an item really moves.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Amounts Automation";
        Source = rig.Cargo(2, "Amounts A");
        Target = rig.Cargo(3, "Amounts B");
        MovedOn = rig.Light(8, "Moved On");
        deck.Lay(new List<Fitting>
        {
            Source, Target, rig.Sprocket, rig.Panel(4, "Ingots Button"), rig.Panel(5, "Plates Button"), rig.Panel(6, "All Button"),
            rig.Panel(7, "Empty Button"), MovedOn, rig.Battery(9, "Amounts Battery")
        }, Vector3I.Zero, 40);

        // In the gap between A and B, level with the middle of their faces, where each has a conveyor port.
        deck.Conveyor(deck[Source].Min + new Vector3I(3, 1, 1));
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Amounts");
        Moving(sim, "Ingots Button", "Ingot/Iron", "100", "hold");
        Next(sim);
        Moving(sim, "Plates Button", "Component/SteelPlate", "2.5", "hold");
        Next(sim);
        Moving(sim, "All Button", "", "", "hold");
        Next(sim);
        Moving(sim, "Empty Button", "Ingot/Iron", "100", "move on");
        Power(sim, "Moved On", "on");

        editor.GoToRoot();
        editor.SwitchSystem(editor.Program.Systems[0], true);
        sim.Tick();
    }

    /// <summary>Goes back to the logic system's screen, for the next sequence.</summary>
    static void Next(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.GoToRoot();
        sim.Tick();
        editor.Push(new LogicSystemPage(editor, editor.Program.Systems[0]));
        sim.Tick();
    }

    /// <summary>
    /// A sequence started by the first button of <paramref name="button" /> that moves <paramref name="amount" /> of
    /// <paramref name="item" /> from Amounts A to Amounts B, or every item where both are empty, doing <paramref name="stuck" /> when it cannot.
    /// </summary>
    static void Moving(Machine.Simulation sim, string button, string item, string amount, string stuck)
    {
        var editor = sim.Editor;
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(button);
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();

        editor.PressAdd("task");
        editor.PressBlock("Amounts A");
        sim.Tick();
        editor.PressChoice("move items");
        sim.Tick();
        var form = editor.Pick().Arguments!.Form;
        ((SlotsField)form.Fields[0]).Select();
        sim.Tick();
        editor.PressBlock("Amounts B");
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();
        form.Fields[1].Text = item;
        form.Fields[2].Text = amount;
        form.Fields[3].Text = stuck;
        editor.PressPrimary();
        sim.Tick();
    }
}
