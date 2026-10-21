using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>Adding, renaming and removing a logic system's types, choices and states, each a change undo takes back.</summary>
public class StateEditTests
{
    const string Text = "sprocket 1 alpha 18\n\n"
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
        + "    type side = inside | outside | between\n"
        + "    type stage = idle | busy\n\n"
        + "    let where = inside\n"
        + "    let home = outside\n"
        + "    let doing = idle\n"
        + "    let cycling = off\n\n"
        + "    when where == outside then set cycling to off\n\n"
        + "    sequence 1 {\n"
        + "        if where != home then {\n"
        + "            set where to outside\n"
        + "        }\n"
        + "    }\n"
        + "}\n";

    static readonly Values Store = new Values();

    static AutomationSystem Loaded()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Text, program, Store));
        return program.Systems[0];
    }

    /// <summary>The system as the script writes it, read back clean.</summary>
    static string Written(AutomationSystem system)
    {
        var text = ProgramWriter.Blueprint(system);
        Assert.Equal("", ProgramReader.Load(text, new Automation(), Store));
        return text;
    }

    /// <summary>Renaming a state reaches every test, set and trigger naming it, and undo takes every one back.</summary>
    [Fact]
    public void RenamingAStateReachesEveryMention()
    {
        var system = Loaded();
        var before = Written(system);
        var rename = new RenameState(system.FindState("where")!, "place");

        rename.Apply(system);
        var after = Written(system);
        Assert.Contains("let place = inside", after);
        Assert.Contains("when place == outside then", after);
        Assert.Contains("if place != home then", after);
        Assert.Contains("set place to outside", after);
        Assert.DoesNotContain("where", after);

        Assert.True(rename.Undoable(system));
        rename.Undo(system);
        Assert.Equal(before, Written(system));
    }

    /// <summary>Renaming a choice reaches the starting values, the values set and the values compared, of its own type alone.</summary>
    [Fact]
    public void RenamingAnOptionReachesItsTypesUses()
    {
        var system = Loaded();
        var before = Written(system);
        var rename = new RenameChoice(system.FindType("side")!, "outside", "out");

        Assert.True(rename.Changes);
        rename.Apply(system);
        var after = Written(system);
        Assert.Contains("type side = inside | out | between", after);
        Assert.Contains("let home = out", after);
        Assert.Contains("when where == out then", after);
        Assert.Contains("set where to out", after);

        rename.Undo(system);
        Assert.Equal(before, Written(system));
    }

    /// <summary>Renaming a type renames it on every state that holds it.</summary>
    [Fact]
    public void RenamingATypeReachesItsStates()
    {
        var system = Loaded();
        var rename = new RenameType(system.FindType("side")!, "place");

        rename.Apply(system);
        Assert.Equal("place", system.FindState("where")!.Type);
        Assert.Contains("type place = inside | outside | between", Written(system));

        rename.Undo(system);
        Assert.Equal("side", system.FindState("where")!.Type);
    }

    /// <summary>A state, choice or type something still uses is not removed, and one nothing uses is, and comes back in its place.</summary>
    [Fact]
    public void WhatIsStillUsedIsNotRemoved()
    {
        var system = Loaded();
        Assert.False(new RemoveState(system, system.FindState("where")!).Changes);
        Assert.False(new RemoveType(system, system.FindType("side")!).Changes);
        Assert.False(new RemoveChoice(system, system.FindType("side")!, "outside").Changes);
        Assert.False(new RemoveChoice(system, system.FindType("stage")!, "busy").Changes);

        var doing = system.FindState("doing")!;
        var removeState = new RemoveState(system, doing);
        Assert.True(removeState.Changes);
        removeState.Apply(system);
        Assert.Null(system.FindState("doing"));

        var removeType = new RemoveType(system, system.FindType("stage")!);
        Assert.True(removeType.Changes);
        removeType.Apply(system);
        removeType.Undo(system);
        removeState.Undo(system);
        Assert.Same(doing, system.States[2]);

        var between = new RemoveChoice(system, system.FindType("side")!, "between");
        Assert.True(between.Changes);
        between.Apply(system);
        Assert.Equal(new[] { "inside", "outside" }, system.FindType("side")!.Choices);
        between.Undo(system);
        Assert.Equal(new[] { "inside", "outside", "between" }, system.FindType("side")!.Choices);
    }

    /// <summary>Adding a type, a choice and a state puts each in its place, and undo takes it out.</summary>
    [Fact]
    public void AddingPutsEachInItsPlace()
    {
        var system = Loaded();
        var valve = new DeclaredType("valve");
        valve.Choices.Add("open");
        valve.Choices.Add("shut");
        var addType = new AddType(valve, 1);
        addType.Apply(system);
        Assert.Same(valve, system.Types[1]);

        var addChoice = new AddChoice(valve, "stuck", 2);
        addChoice.Apply(system);
        var intake = new State("intake", "valve", Literal.Name("stuck"));
        var addState = new AddState(intake, 0);
        addState.Apply(system);
        Assert.Contains("let intake = stuck", Written(system));

        Assert.False(addChoice.Undoable(system));
        addState.Undo(system);
        Assert.True(addChoice.Undoable(system));
        addChoice.Undo(system);
        addType.Undo(system);
        Assert.Null(system.FindType("valve"));
    }

    /// <summary>A starting value changes within its type, and to another type only where nothing mentions the state.</summary>
    [Fact]
    public void AStartingValueChangesTypeOnlyWhereNothingUsesTheState()
    {
        var system = Loaded();
        var where = system.FindState("where")!;
        var between = new ChangeStart(system, where, Literal.Name("between"), "side");
        Assert.True(between.Changes);
        between.Apply(system);
        Assert.Contains("let where = between", Written(system));
        between.Undo(system);
        Assert.Equal(Literal.Name("inside"), where.Start);

        Assert.False(new ChangeStart(system, where, Literal.Of(true), "").Changes);

        var doing = system.FindState("doing")!;
        var flag = new ChangeStart(system, doing, Literal.Of(true), "");
        Assert.True(flag.Changes);
        flag.Apply(system);
        Assert.True(doing.IsFlag);
        Assert.Contains("let doing = on", Written(system));
    }
}
