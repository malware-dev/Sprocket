using Sprocket.Host.SpaceEngineers;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System.Collections.Generic;
using System.Linq;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>A cargo container and a refinery, whose stock flashes a light as it passes a level.</summary>
public sealed class StockWatch : Scenario
{
    /// <summary>The cargo container, which holds one inventory.</summary>
    public Stockroom Cargo { get; private set; } = null!;

    /// <summary>The refinery, which holds an input and an output.</summary>
    public Stockroom Refinery { get; private set; } = null!;

    /// <inheritdoc />
    public override string Name => "Sprocket Stock Watch";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("Stock in a cargo container fires its trigger",
            "Sprocket Stock Watch pasted, its Stock logic system switched on, in creative mode.",
            new[]
            {
                "Put 200 Iron Ingot in Stock Cargo by hand.",
                "Take it all out, then put 200 in again."
            },
            "Stock Light comes on for 3 seconds at each 200 put in, and the line reads \"the amount of Iron Ingot in Stock Cargo is above 100 kg\".",
            "The harness raises the inventory's event itself; only the game shows it is raised for a hand moving items."),
        new GameCheck("Stock in a refinery's input fires its trigger",
            "Sprocket Stock Watch pasted, its Stock logic system switched on, in creative mode, Stock Refinery switched off.",
            new[]
            {
                "Put 200 Iron Ore in Stock Refinery's input by hand.",
                "Take it all out, then put 200 in again."
            },
            "Stock Light comes on for 3 seconds at each 200 put in, and the line reads \"the amount of Iron Ore in the input of Stock Refinery is above 100 kg\".",
            "Only the game has a refinery's real input, and its event.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Stock Automation";
        Cargo = rig.Cargo(2, "Stock Cargo");
        Refinery = rig.Refinery(3, "Stock Refinery");
        deck.Lay(new List<Fitting> { rig.Sprocket, Cargo, Refinery, rig.Light(4, "Stock Light"), rig.Battery(5, "Stock Battery") }, Vector3I.Zero, 8);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Stock");
        Watch(sim, "Stock Cargo", null, "Ingot/Iron");
        Flash(sim, 0);
        editor.GoToRoot();
        sim.Tick();

        var system = editor.Program.Systems[0];
        editor.Push(new LogicSystemPage(editor, system));
        sim.Tick();
        Watch(sim, "Stock Refinery", "input", "Ore/Iron");
        Flash(sim, 1);
        editor.GoToRoot();
        editor.SwitchSystem(system, true);
        sim.Tick();
    }

    /// <summary>Starts a sequence when the amount of <paramref name="item" /> on <paramref name="block" /> passes 100, in its <paramref name="inventory" /> where it asks.</summary>
    static void Watch(Machine.Simulation sim, string block, string? inventory, string item)
    {
        var editor = sim.Editor;
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(block);
        sim.Tick();
        editor.Pick().Stages[1].Entries().First(entry => entry.Text == "amount" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        sim.Tick();
        var form = editor.Pick().Arguments!.Form;
        if (inventory != null)
            form.Fields[0].Text = inventory;

        form.Fields[1].Text = item;
        form.Fields[2].Text = "is above";
        form.Fields[3].Text = "100";
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();
    }

    /// <summary>Gives the sequence numbered <paramref name="at" /> a light on for 3 seconds, ending as it starts.</summary>
    static void Flash(Machine.Simulation sim, int at)
    {
        var editor = sim.Editor;
        editor.Push(new SequencePage(editor, editor.Program.Systems[0].Sequences[at]));
        sim.Tick();
        Task(editor, "on");
        editor.PressAdd("delay");
        for (var i = 0; i < 3; i++)
            sim.Tick();
        var length = ((SequencePage)editor.Current).Lengths!.Box;
        length.Text = "3";
        length.Submit!.Execute();
        sim.Tick();
        Task(editor, "off");
        sim.Tick();
    }

    static void Task(SprocketEditor editor, string power)
    {
        editor.PressAdd("task");
        editor.PressBlock("Stock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = power;
        editor.PressPrimary();
    }
}
