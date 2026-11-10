using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>Three cargo containers, the first two joined by a conveyor junction, two buttons that move every item out of the first, and a light the move to the second switches on once it is done.</summary>
public sealed class MoveItems : Scenario
{
    /// <summary>The container the items start in.</summary>
    public Stockroom Source { get; private set; } = null!;

    /// <summary>The container joined to the source by a conveyor.</summary>
    public Stockroom Joined { get; private set; } = null!;

    /// <summary>The container nothing joins to the source.</summary>
    public Stockroom Apart { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Move Items";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("Items move only over a real conveyor connection",
            "Sprocket Move Items pasted, its Move logic system switched on. Iron ingots in Move A.",
            new[] { "Press To B Button.", "Put the ingots back in Move A, and press To C Button." },
            "To B, every ingot moves and B Done comes on. To C, nothing moves, since no conveyor joins it to Move A.",
            "The harness models no conveyors, so only the game's own connection check says what can move.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Move Automation";
        Source = rig.Cargo(2, "Move A");
        Joined = rig.Cargo(3, "Move B");
        Apart = rig.Cargo(4, "Move C");
        deck.Lay(new List<Fitting> { Source, Joined, Apart, rig.Sprocket, rig.Panel(5, "To B Button"), rig.Panel(6, "To C Button"), rig.Light(8, "B Done"), rig.Battery(7, "Move Battery") }, Vector3I.Zero, 40);

        // In the gap between A and B, level with the middle of their faces, where each has a conveyor port.
        deck.Conveyor(deck[Source].Min + new Vector3I(3, 1, 1));
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Move");
        Moving(sim, "To B Button", "Move B");
        Power(sim, "B Done", "on");
        editor.GoToRoot();
        sim.Tick();

        var system = editor.Program.Systems[0];
        editor.Push(new LogicSystemPage(editor, system));
        sim.Tick();
        Moving(sim, "To C Button", "Move C");
        editor.GoToRoot();
        editor.SwitchSystem(system, true);
        sim.Tick();
    }

    /// <summary>A sequence started by the first button of <paramref name="button" /> that moves every item from Move A to <paramref name="target" />.</summary>
    static void Moving(Machine.Simulation sim, string button, string target)
    {
        var editor = sim.Editor;
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(button);
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();

        editor.PressAdd("task");
        editor.PressBlock("Move A");
        sim.Tick();
        editor.PressChoice("move items");
        sim.Tick();
        ((SlotsField)editor.Pick().Arguments!.Form.Fields[0]).Select();
        sim.Tick();
        editor.PressBlock(target);
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();
    }
}
