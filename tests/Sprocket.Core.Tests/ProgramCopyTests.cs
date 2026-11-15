using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using VRageMath;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A program copied in memory says exactly what its original says, and is a program of its own.</summary>
public class ProgramCopyTests
{
    /// <summary>A program using every part of the format writes out the same copied as it does itself, switches and all.</summary>
    [Fact]
    public void ACopyWritesOutAsItsOriginal()
    {
        var values = new Values();
        var original = Everything(values);

        var into = Crowded();
        var copy = new Automation();
        ProgramCopy.Into(original, values, copy, into);

        Assert.Equal(ProgramWriter.Save(original, values), ProgramWriter.Save(copy, into));
        Assert.Equal(Switches(original), Switches(copy));
    }

    /// <summary>The program the first test copies holds a step of every kind and a trigger of every kind there is.</summary>
    [Fact]
    public void EveryKindOfStepAndTriggerIsCopied()
    {
        var program = Everything(new Values());
        var steps = new HashSet<Type>();
        var triggers = new HashSet<Type>();
        foreach (var system in program.Systems)
        {
            foreach (var trigger in system.Triggers)
            {
                triggers.Add(trigger.GetType());
                steps.Add(trigger.Does.GetType());
            }

            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                    steps.Add(step.GetType());
            }
        }

        var kinds = typeof(Step).Assembly.GetTypes().Where(type => !type.IsAbstract).ToList();
        Assert.DoesNotContain(kinds, type => typeof(Step).IsAssignableFrom(type) && !steps.Contains(type));
        Assert.DoesNotContain(kinds, type => typeof(Trigger).IsAssignableFrom(type) && !triggers.Contains(type));
    }

    /// <summary>A copy changed leaves its original as it was.</summary>
    [Fact]
    public void ChangingACopyLeavesTheOriginal()
    {
        var values = new Values();
        var original = Everything(values);
        var before = ProgramWriter.Save(original, values);

        var into = new Values();
        var copy = new Automation();
        ProgramCopy.Into(original, values, copy, into);

        var system = copy.Systems[0];
        system.Name = "Changed";
        system.Parts[0].Category = "More";
        system.Parts[0].Short = "Other";
        ((Act)system.Sequences[0].Steps[0]).Targets.Add("door2");
        ((WhenTrigger)system.Triggers[1]).Sources.Add("door2");
        system.Instances[0].Fill("inner", new List<string> { "door2" });
        copy.Refs[0].Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(99L)));
        copy.SignalRefs[0].Blocks.Add("door2");
        copy.Systems[1].Sequences[0].Steps.Clear();

        Assert.Equal(before, ProgramWriter.Save(original, values));
    }

    /// <summary>The switches a program's block keeps.</summary>
    static string Switches(Automation program)
    {
        var state = BlockSwitches.Of(program);
        return string.Join(";", state.Switches.Select(off => $"{off.System:D}/{off.Instance}/{off.Halt != null}"));
    }

    /// <summary>A values table already holding text, so text copied into it lands in other slots than it had.</summary>
    static Values Crowded()
    {
        var values = new Values();
        for (var i = 0; i < 5; i++)
            values.Text("filler " + i);

        return values;
    }

    /// <summary>
    /// A program using every part of the format: blocks with every kind of key, a signal, a blueprint with every kind of slot,
    /// a type, a state, a trigger of every kind, a step of every kind, installations answering every kind of slot, a system
    /// and an installation switched off, dates, notes and attributes.
    /// </summary>
    static Automation Everything(Values values)
    {
        var program = new Automation();
        program.Annotation = "The whole format.";
        program.Attributes = Attributes("programNote", "1");

        var inner = new EntityRef("inner1", "Door/LargeBlockDoor");
        inner.Annotation = "The inner door.";
        inner.Attributes = Attributes("doorNote", "\"inner\"");
        inner.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(74857693847562934L)));
        inner.Keys.Add(new EntityKey(new[] { new Vector3I(0, 2, -1), new Vector3I(3, 0, 0) }));
        inner.Keys.Add(new EntityKey(ValueKind.Text, values.Text("Airlock Inner Door")));
        inner.Keys.Add(new EntityKey(ValueKind.Real, EntityValue.Of(2.5)));
        inner.Keys.Add(new EntityKey(ValueKind.Flag, EntityValue.Of(true)));
        inner.Keys.Add(new EntityKey(ValueKind.Color, EntityValue.Of(255, 128, 0, 255)));
        program.Restore(inner);

        var door2 = new EntityRef("door2", "Door/LargeBlockDoor");
        door2.Keys.Add(new EntityKey(ValueKind.Text, values.Text("Airlock Outer Door")));
        program.Restore(door2);

        var panel = new EntityRef("panel1", "ButtonPanel/ButtonPanelLarge");
        panel.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(74857693847562937L)));
        program.Restore(panel);

        var lcd = new EntityRef("lcd1", "TextPanel/LargeLCDPanel");
        lcd.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(5L)));
        program.Restore(lcd);

        var button = new SignalRef("button", new List<string> { "panel1" }, new ToolbarSlot(1, 1, 4));
        button.Annotation = "The airlock's button.";
        button.Attributes = Attributes("buttonNote", "2");
        program.Restore(button);

        Blueprint(program);
        Plain(program);
        return program;
    }

    /// <summary>A blueprint with every kind of slot, a type and a state, its triggers and steps, and two installations.</summary>
    static void Blueprint(Automation program)
    {
        var system = program.Restore(new Guid("3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43"), "MalTek", "Airlock");
        system.IsBlueprint = true;
        system.Annotation = "Cycles the airlock.";
        system.Attributes = Attributes("systemNote", "\"airlock\"");
        system.Changed = new DateTime(2026, 9, 14, 19, 2, 11, DateTimeKind.Utc);
        system.Origin = new Guid("9d0f1a2b-3c4d-4e5f-8a6b-7c8d9e0f1a2b");
        system.OriginChanged = new DateTime(2026, 9, 13, 8, 0, 0, DateTimeKind.Utc);

        var door = system.PartFor("inner");
        door.Annotation = "The door inside.";
        door.Attributes = Attributes("slotNote", "3");
        door.Short = "Inner";
        door.Category = "Doors";
        door.Expects = "Door";

        var pressed = system.PartFor("press");
        pressed.IsSignal = true;
        pressed.Optional = true;

        var screen = system.PartFor("status");
        screen.Holds = HostTypes.All[0].Id;
        screen.Category = "Screens";

        var side = new DeclaredType("side");
        side.Annotation = "Where somebody is.";
        side.Attributes = Attributes("typeNote", "4");
        side.Choices.Add("inside");
        side.Choices.Add("outside");
        system.Types.Add(side);

        var where = new State("where", "side", Literal.Name("inside"));
        where.Annotation = "Where the last person went.";
        where.Attributes = Attributes("stateNote", "5");
        system.States.Add(where);

        var cycle = program.Restore(system, 1, null);
        cycle.Name = "Cycle";
        cycle.Annotation = "In or out.";
        cycle.Attributes = Attributes("sequenceNote", "6");

        var close = new Act("close") { Provider = BlockKinds.Own, Form = ReachKind.Action };
        close.Targets.Add("inner");
        close.Annotation = "Shut it first.";
        close.Attributes = Attributes("stepNote", "7");
        cycle.Steps.Add(close);

        var opened = new Comparison { MemberId = "open", Provider = "", Quantifier = Quantifier.All, Relation = Relation.Is, Value = Literal.Of(true) };
        opened.Targets.Add("inner");
        cycle.Steps.Add(new If(opened));
        cycle.Steps.Add(new Wait(Duration.OfTicks(60)));
        cycle.Steps.Add(new Else(new StateTest { State = "where", Relation = Relation.IsNot, Value = Literal.Name("outside") }));
        cycle.Steps.Add(new Await());
        cycle.Steps.Add(new Abort { Message = "Door \"B\" jammed", Level = AbortLevel.Problem });
        cycle.Steps.Add(new Else(null));
        cycle.Steps.Add(new SetState { State = "where", Value = Literal.Name("outside") });
        cycle.Steps.Add(new EndIf());

        var off = new Act("open") { Provider = BlockKinds.Own, Form = ReachKind.Action, IsOff = true };
        off.Targets.Add("inner");
        cycle.Steps.Add(off);

        var lit = new Act("Brightness") { Form = ReachKind.Value };
        lit.Targets.Add("inner");
        lit.Arguments.Add(Literal.Of(0.5));
        lit.Arguments.Add(Literal.Of("on"));
        cycle.Steps.Add(lit);
        cycle.Steps.Add(new Catch { Annotation = "Undo what was done." });
        cycle.Steps.Add(new Wait(Duration.OfTicks(5)));
        cycle.Steps.Add(new Finally());
        cycle.Steps.Add(new SetState { State = "where", Value = Literal.Name("inside") });

        var again = program.Restore(system, 3, null);
        again.Steps.Add(new Run(1, true));
        again.Steps.Add(new Run(1));
        system.Used(5);

        var press = SignalTrigger.Named("press");
        press.Does = new Run(1);
        press.Annotation = "The button starts it.";
        press.Attributes = Attributes("triggerNote", "8");
        system.AddTrigger(press);

        var shut = new WhenTrigger("open") { Quantifier = Quantifier.Any, Relation = Relation.Is, Value = Literal.Of(false), Interval = Duration.OfTicks(10) };
        shut.Sources.Add("inner");
        shut.Does = new Run(3);
        system.AddTrigger(shut);

        var finished = SignalTrigger.Written(BlockKinds.Own, "finishes_closing");
        finished.Sources.Add("inner");
        finished.Does = new SetState { State = "where", Value = Literal.Name("inside") };
        system.AddTrigger(finished);

        var outside = new StateTrigger(new StateTest { State = "where", Relation = Relation.Is, Value = Literal.Name("outside") });
        outside.Does = new Run(1, true);
        system.AddTrigger(outside);

        var port = system.Restore(1, "Port");
        port.Annotation = "The port airlock.";
        port.Attributes = Attributes("instanceNote", "9");
        port.Fill("inner", new List<string> { "inner1" });
        port.Note("inner", "The port side's inner door.");
        port.Keep("inner", Attributes("answerNote", "10"));
        port.Fill("press", new List<string> { "panel1" }, new List<ToolbarSlot> { new ToolbarSlot(1, 2, 3) });
        port.Fill("status", new List<string> { "lcd1" }, new List<int> { 2 });

        var starboard = system.Restore(4, "");
        starboard.Fill("inner", new List<string> { "door2" });
        starboard.Enabled = false;
        starboard.Halt = new Halt(SequenceProblem.MissingTarget, 1, 0, default(BlockTrouble), "door2");
    }

    /// <summary>A system of its own, switched off, started by the signal variable and on a clock.</summary>
    static void Plain(Automation program)
    {
        var system = program.Restore(new Guid("1b2c3d4e-5f60-4718-9a2b-3c4d5e6f7081"), "", "Lights");

        var sequence = program.Restore(system, 1, null);
        var on = new Act("OnOff_On");
        on.Targets.Add("door2");
        sequence.Steps.Add(on);

        var pressed = SignalTrigger.Named("button");
        pressed.Does = new Run(1);
        system.AddTrigger(pressed);

        var clock = new EveryTrigger(600);
        clock.Does = new Run(1, true);
        system.AddTrigger(clock);

        system.Enabled = false;
    }

    /// <summary>Attributes Sprocket does not use, kept as written: one called <paramref name="name" />, written <paramref name="written" />.</summary>
    static KeptAttributes Attributes(string name, string written)
    {
        var kept = new KeptAttributes();
        kept.Add(name, written);
        return kept;
    }
}
