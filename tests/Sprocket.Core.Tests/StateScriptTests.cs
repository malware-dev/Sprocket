using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A logic system's types and states, and the steps, tests and triggers on them, read and written back.</summary>
public class StateScriptTests
{
    const string Version = "sprocket 1 alpha 18\n\n";

    /// <summary>The spec's airlock, a system blueprint with a type, two states, a when on a state and an if on one.</summary>
    const string Airlock = Version
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
        + "    required slot inner expects Door\n"
        + "    required slot outer expects Door\n"
        + "    optional slot lamp expects LightingBlock\n\n"
        + "    type side = inside | outside\n\n"
        + "    let where = inside\n"
        + "    let cycling = off\n\n"
        + "    when where == outside then run 2\n\n"
        + "    sequence 1 as Cycle {\n"
        + "        if where == inside then {\n"
        + "            set cycling to on\n"
        + "            call sprocket.close on [ inner ]\n"
        + "            await\n"
        + "            call sprocket.open on [ outer ]\n"
        + "            set where to outside\n"
        + "            set cycling to off\n"
        + "        }\n"
        + "    }\n\n"
        + "    sequence 2 as Outside {\n"
        + "        call SetColor(255, 136, 0) on [ lamp ]\n"
        + "    }\n"
        + "}\n";

    static readonly Values Store = new Values();

    static string Load(string text, Automation program) => ProgramReader.Load(text, program, Store);

    static string Save(Automation program) => ProgramWriter.Save(program, Store);

    /// <summary>The spec's airlock reads clean, holds its type and states, and is written back as it was.</summary>
    [Fact]
    public void TheSpecsAirlockReadsAndWritesBackAsItWas()
    {
        var program = new Automation();
        Assert.Equal("", Load(Airlock, program));

        var system = program.Systems[0];
        var side = Assert.Single(system.Types);
        Assert.Equal("side", side.Name);
        Assert.Equal(new[] { "inside", "outside" }, side.Choices);
        Assert.Equal("side", system.FindState("where")!.Type);
        Assert.Equal(Literal.Name("inside"), system.FindState("where")!.Start);
        Assert.True(system.FindState("cycling")!.IsFlag);
        Assert.Equal(Literal.Of(false), system.FindState("cycling")!.Start);

        var watched = Assert.IsType<StateTrigger>(Assert.Single(system.Triggers));
        Assert.Equal("where", watched.Test.State);
        Assert.Equal(Literal.Name("outside"), watched.Test.Value);
        Assert.Equal(2, Assert.IsType<Run>(watched.Does).Number);

        var steps = system.FindSequence(1)!.Steps;
        var tested = Assert.IsType<StateTest>(Assert.IsType<If>(steps[0]).Test);
        Assert.Equal(Relation.Is, tested.Relation);
        var sets = Assert.IsType<SetState>(steps[5]);
        Assert.Equal("where", sets.State);
        Assert.Equal(Literal.Name("outside"), sets.Value);

        var written = Save(program);
        Assert.Contains("    type side = inside | outside\n", written);
        Assert.Contains("    let where = inside\n    let cycling = off\n", written);
        Assert.Contains("    when where == outside then run 2\n", written);
        Assert.Contains("        if where == inside then {\n", written);
        Assert.Contains("            set where to outside\n", written);

        var again = new Automation();
        Assert.Equal("", Load(written, again));
        Assert.Equal(written, Save(again));
    }

    /// <summary>An if on a state and one on blocks chain either way round, and a state is compared with another of its type.</summary>
    [Fact]
    public void AnIfOnAStateAndOneOnBlocksChainEitherWay()
    {
        var text = Version
            + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
            + "    required slot outer expects Door\n\n"
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n"
            + "    let home = outside\n\n"
            + "    sequence 1 as Cycle {\n"
            + "        if where != home then {\n"
            + "            call sprocket.close on [ outer ]\n"
            + "        }\n"
            + "        else if all sprocket.status on [ outer ] == closed then {\n"
            + "            set where to home\n"
            + "        }\n"
            + "        else if where == side(outside) then {\n"
            + "            await\n"
            + "        }\n"
            + "        else {\n"
            + "            call sprocket.close on [ outer ]\n"
            + "        }\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));

        var steps = program.Systems[0].FindSequence(1)!.Steps;
        var first = Assert.IsType<StateTest>(Assert.IsType<If>(steps[0]).Test);
        Assert.Equal("home", first.Other);
        Assert.Equal(Relation.IsNot, first.Relation);
        Assert.IsType<Comparison>(Assert.IsType<Else>(steps[2]).Test);
        Assert.Equal("home", Assert.IsType<SetState>(steps[3]).From);
        Assert.Equal(Literal.Name("outside"), Assert.IsType<StateTest>(Assert.IsType<Else>(steps[4]).Test).Value);

        var written = Save(program);
        var again = new Automation();
        Assert.Equal("", Load(written, again));
        Assert.Equal(written, Save(again));
    }

    /// <summary>
    /// A choice is written qualified where alone it would read as something else: where a state has its name, and as a
    /// starting value where another type has it too.
    /// </summary>
    [Fact]
    public void AnOptionIsQualifiedWhereAloneItWouldReadAsSomethingElse()
    {
        var text = Version
            + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
            + "    type valve = open | shut\n"
            + "    type hatch = open | shut\n"
            + "    type side = inside | outside\n\n"
            + "    let intake = valve(open)\n"
            + "    let outside = off\n"
            + "    let where = side(outside)\n\n"
            + "    sequence 1 {\n"
            + "        set where to side(outside)\n"
            + "        set intake to shut\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));

        var written = Save(program);
        Assert.Contains("    let intake = valve(open)\n", written);
        Assert.Contains("    let where = side(outside)\n", written);
        Assert.Contains("        set where to side(outside)\n", written);
        Assert.Contains("        set intake to shut\n", written);

        var again = new Automation();
        Assert.Equal("", Load(written, again));
        Assert.Equal(written, Save(again));
    }

    /// <summary><c>type</c> is one of the language's words, so a slot of that name is written quoted and read back.</summary>
    [Fact]
    public void ASlotCalledTypeIsQuoted()
    {
        var text = Version
            + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
            + "    required slot 'type' expects Door\n\n"
            + "    sequence 1 {\n"
            + "        call sprocket.close on [ 'type' ]\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        Assert.Contains("required slot 'type' expects Door", Save(program));
    }

    /// <summary>Each line the spec makes an error is trouble, and says why.</summary>
    [Theory]
    [InlineData("    type side = inside\n", "needs at least two choices")]
    [InlineData("    type side = inside | inside\n", "written twice")]
    [InlineData("    type side inside | outside\n", "expected '='")]
    [InlineData("    type side = inside outside\n", "expected '|'")]
    [InlineData("    type flag = inside | outside\n", "built-in type")]
    [InlineData("    type side = inside | outside\n    type side = up | down\n", "already the name of a type")]
    [InlineData("    let where = nowhere\n", "no choice 'nowhere'")]
    [InlineData("    let where = #FF0000FF\n", "a state starts as on, off, a number, a text, a length of time or one of the system's choices")]
    [InlineData("    let where = on\n    let where = off\n", "already the name of a state")]
    [InlineData("    type valve = open | shut\n    type hatch = open | shut\n    let intake = open\n", "write the one meant with its kind: valve(open) or hatch(open)")]
    [InlineData("    type side = inside | outside\n    let where = inside\n    let home = where\n", "'where' is a state")]
    [InlineData("    type side = inside | outside\n    let where = side(nowhere)\n", "no choice 'nowhere' in type 'side'")]
    [InlineData("    type side = inside | outside\n    let where = Gearbox(closed)\n", "no type 'Gearbox'")]
    [InlineData("    let cycling = off\n    sequence 1 {\n        set nothing to on\n    }\n", "no state 'nothing'")]
    [InlineData("    let cycling = off\n    sequence 1 {\n        set cycling to inside\n    }\n", "holds on or off")]
    [InlineData("    type side = inside | outside\n    let where = inside\n    sequence 1 {\n        set where to on\n    }\n", "not a flag")]
    [InlineData("    type side = inside | outside\n    type stage = idle | busy\n    let where = inside\n    let doing = idle\n    sequence 1 {\n        set where to doing\n    }\n", "of another type")]
    [InlineData("    type side = inside | outside\n    let where = inside\n    sequence 1 {\n        if where > inside then {\n        }\n    }\n", "expected == or !=")]
    [InlineData("    type side = inside | outside\n    let where = inside\n    sequence 1 {\n        if where == inside {\n        }\n    }\n", "expected 'then'")]
    [InlineData("    type side = inside | outside\n    let where = inside\n    when where == outside every 10t then run 1\n    sequence 1 {\n    }\n", "takes no every")]
    [InlineData("    sequence 1 {\n        set where to on\n    }\n    let where = off\n", "no state 'where' declared above this line")]
    public void AnErrorTheSpecNamesIsTrouble(string body, string why)
    {
        var text = Version
            + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
            + body
            + "}\n";

        var trouble = Load(text, new Automation());
        Assert.Contains(why, trouble);
    }

    /// <summary>A program of the draft whose states first held numbers, texts and lengths, the second of version 3, reads and is written in the current draft.</summary>
    [Fact]
    public void AProgramOfTheStateTypesDraftReadsInTheCurrentOne()
    {
        var program = new Automation();
        Assert.Equal("", Load(Counter.Replace("sprocket 3 alpha 3\n", "sprocket 3 alpha 2\n"), program));
        Assert.StartsWith(Host.SpaceEngineers.Framework.Language.Format.ScriptFormat.Line + "\n", ProgramWriter.Save(program, Store));
    }

    /// <summary>A program with states of every built-in type the spec names, set and compared as the spec allows.</summary>
    const string Counter = "sprocket 3 alpha 3\n\n"
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Counter\n{\n"
        + "    optional slot Limit expects integer = 10\n\n"
        + "    let count = 0\n"
        + "    let level = 0.0\n"
        + "    let share = real(2)\n"
        + "    let label = \"Idle\"\n"
        + "    let pause = 2s\n"
        + "    let most = Limit\n"
        + "    let other = 3\n\n"
        + "    when count >= 5 then run 1\n\n"
        + "    sequence 1 {\n"
        + "        set count to count + 1\n"
        + "        set level to 4\n"
        + "        set label to \"Busy\"\n"
        + "        set pause to 1.5s\n"
        + "        set count to other\n"
        + "        set count to Limit\n"
        + "        if count < most then {\n"
        + "            set level to 2.5\n"
        + "        }\n"
        + "        if label == \"Busy\" then {\n"
        + "            set label to \"Idle\"\n"
        + "        }\n"
        + "    }\n"
        + "}\n";

    /// <summary>
    /// A state takes the type of its starting value: a whole number an integer, <c>0.0</c> and <c>real(2)</c> a real, a text
    /// and a length their own, a setting the type it holds; the program is written back as it was, a real state's whole
    /// start written as a real.
    /// </summary>
    [Fact]
    public void AStateHoldsTheTypeOfItsStartingValue()
    {
        var program = new Automation();
        Assert.Equal("", Load(Counter, program));

        var system = program.Systems[0];
        Assert.Equal(ValueKind.Integer, system.FindState("count")!.Kind);
        Assert.Equal(ValueKind.Real, system.FindState("level")!.Kind);
        Assert.Equal(ValueKind.Real, system.FindState("share")!.Kind);
        Assert.Equal(ValueKind.Text, system.FindState("label")!.Kind);
        Assert.Equal(ValueKind.Duration, system.FindState("pause")!.Kind);
        Assert.Equal(ValueKind.Integer, system.FindState("most")!.Kind);
        Assert.True(system.FindState("count")!.IsOrdered);
        Assert.False(system.FindState("label")!.IsOrdered);

        Assert.Equal(Counter.Replace("let share = real(2)", "let share = 2.0"), Save(program));
    }

    /// <summary>What a built-in state is given or compared with that its type does not hold is an error that says what it holds.</summary>
    [Theory]
    [InlineData("    let count = 0\n    sequence 1 {\n        set count to \"five\"\n    }\n", "state 'count' holds an integer")]
    [InlineData("    let count = 0\n    sequence 1 {\n        set count to 2.5\n    }\n", "state 'count' holds an integer")]
    [InlineData("    let label = \"Idle\"\n    sequence 1 {\n        set label to 5\n    }\n", "state 'label' holds text")]
    [InlineData("    let label = \"Idle\"\n    sequence 1 {\n        if label > \"a\" then {\n        }\n    }\n", "which has no order")]
    [InlineData("    let count = 0\n    let label = \"Idle\"\n    sequence 1 {\n        set count to label\n    }\n", "of another type than state 'count'")]
    [InlineData("    let count = text(0)\n", "is not written as a value of text")]
    public void AValueABuiltInStateDoesNotHoldIsTrouble(string body, string why)
    {
        var text = "sprocket 3 alpha 3\n\n"
            + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Counter\n{\n"
            + body
            + "}\n";

        Assert.Contains(why, Load(text, new Automation()));
    }

    /// <summary>In a plain system a state may not take the name of a variable of the file.</summary>
    [Fact]
    public void APlainSystemsStateTakesNoVariablesName()
    {
        var text = Version
            + "let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n\n"
            + "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n"
            + "    let door1 = off\n"
            + "}\n";

        Assert.Contains("already a variable of the file", Load(text, new Automation()));
    }
}
