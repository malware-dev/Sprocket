using Sprocket.Host.SpaceEngineers;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System.Collections.Generic;
using System.Linq;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>Two cargo containers whose mass and fill, read through their inventory slots, switch on a light each side of a level.</summary>
public sealed class InventoryReadings : Scenario
{
    /// <inheritdoc />
    public override string Name => "Sprocket Inventory Readings";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("Reading an inventory's mass and fill through its slot",
            "Sprocket Inventory Readings pasted, its Read logic system switched on.",
            new[] { "Put 1000 kg of iron ingots in Mass Cargo.", "Fill Fill Cargo with stone to half its volume." },
            "Mass Low and Fill Low come on; Mass High and Fill High stay off.",
            "The harness imitates an inventory's mass and fill with made-up numbers; only the game gives its own.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Read Automation";
        deck.Lay(new List<Fitting>
        {
            rig.Sprocket, rig.Cargo(2, "Mass Cargo"), rig.Cargo(3, "Fill Cargo"), rig.Light(4, "Mass Low"), rig.Light(5, "Mass High"),
            rig.Light(6, "Fill Low"), rig.Light(7, "Fill High"), rig.Battery(8, "Read Battery")
        }, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Read");
        var levels = new[]
        {
            ("Mass Cargo", "inventory mass", "990", "Mass Low"), ("Mass Cargo", "inventory mass", "1010", "Mass High"),
            ("Fill Cargo", "inventory fill", "49", "Fill Low"), ("Fill Cargo", "inventory fill", "51", "Fill High")
        };

        for (var i = 0; i < levels.Length; i++)
        {
            if (i > 0)
            {
                editor.GoToRoot();
                editor.Push(new LogicSystemPage(editor, editor.Program.Systems[0]));
                sim.Tick();
            }

            var (block, member, level, light) = levels[i];
            Above(sim, block, member, level);
            On(editor, light);
            sim.Tick();
        }

        editor.GoToRoot();
        editor.SwitchSystem(editor.Program.Systems[0], true);
        sim.Tick();
    }

    /// <summary>Starts a sequence when <paramref name="member" /> of <paramref name="block" /> rises above <paramref name="level" />.</summary>
    static void Above(Machine.Simulation sim, string block, string member, string level)
    {
        var editor = sim.Editor;
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(block);
        sim.Tick();
        editor.Pick().Stages[1].Entries().First(entry => entry.Text == member && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        sim.Tick();
        var form = editor.Pick().Arguments!.Form;
        form.Fields[0].Text = "is above";
        form.Fields[1].Text = level;
        sim.Tick();
        editor.PressPrimary();
        sim.Tick();
    }

    static void On(SprocketEditor editor, string light)
    {
        editor.PressAdd("task");
        editor.PressBlock(light);
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
    }
}
