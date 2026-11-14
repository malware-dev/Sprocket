using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System.Collections.Generic;
using Sprocket.Sim.Blueprints;
using Sprocket.Sim.Driving;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Scenarios;

/// <summary>Two programmable blocks whose scripts a button each runs, and a light each run switches on once its script has run.</summary>
/// <remarks>The scripts are the game test's to write: one that loops without end, and one that returns at once.</remarks>
public sealed class ScriptLimit : Scenario
{
    /// <inheritdoc />
    public override string Name => "Sprocket Script Limit";

    /// <inheritdoc />
    public override IReadOnlyList<GameCheck> Checks => new[]
    {
        new GameCheck("A script over its instruction limit",
            "Sprocket Script Limit pasted, its Script logic system switched on. Looping Script holds a script that loops without end, Plain Script one that returns at once.",
            new[] { "Press Looping Button.", "Press Plain Button." },
            "Looping Button stops at the run and says the script did not run, Looping Ran stays off, the game keeps running, and the log names Looping Script. Plain Button switches Plain Ran on.",
            "Only the game runs a player's script.")
    };

    /// <inheritdoc />
    protected override void Lay(Rig rig, Deck deck)
    {
        rig.Sprocket.Name = "Script Automation";
        deck.Lay(new List<Fitting>
        {
            rig.Programmable(2, "Looping Script"), rig.Programmable(3, "Plain Script"), rig.Sprocket, rig.Panel(4, "Looping Button"),
            rig.Panel(5, "Plain Button"), rig.Light(6, "Looping Ran"), rig.Light(7, "Plain Ran"), rig.Battery(8, "Script Battery")
        }, Vector3I.Zero, 40);
    }

    /// <inheritdoc />
    protected override void Program(Machine.Simulation sim)
    {
        var editor = sim.Editor;
        editor.NewSystem("Script");
        Running(sim, "Looping Button", "Looping Script", "Looping Ran");
        editor.GoToRoot();
        editor.Push(new LogicSystemPage(editor, editor.Program.Systems[0]));
        sim.Tick();
        Running(sim, "Plain Button", "Plain Script", "Plain Ran");
        editor.GoToRoot();
        editor.SwitchSystem(editor.Program.Systems[0], true);
        sim.Tick();
    }

    /// <summary>A sequence started by the first button of <paramref name="button" /> that switches <paramref name="light" /> off, runs <paramref name="script" />, and switches the light on.</summary>
    static void Running(Machine.Simulation sim, string button, string script, string light)
    {
        var editor = sim.Editor;
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock(button);
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        sim.Tick();

        Power(sim, light, "off");
        editor.PressAdd("task");
        editor.PressBlock(script);
        sim.Tick();
        editor.PressChoice("run");
        sim.Tick();
        editor.Pick().Arguments!.Form.Fields[0].Text = "go";
        editor.PressPrimary();
        sim.Tick();
        Power(sim, light, "on");
    }
}
