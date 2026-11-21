using Sprocket.Host.SpaceEngineers.Features.Sharing.Text;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Expressions in a program's text: wherever a step takes a literal it takes operands joined by operators, functions and
/// readings, which read back and are written with only the parentheses precedence needs.
/// </summary>
public class ExpressionScriptTests
{
    const string Identity = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    const string Version = "sprocket 2 alpha 4\n\n";

    const string Head = "system blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n"
        + "    required slot pad expects InteriorLight\n"
        + "    required slot tanks expects InteriorLight\n"
        + "    optional slot Margin expects real = 2\n"
        + "    optional slot Delay expects duration = 1s\n"
        + "    optional slot greeting expects text = \"Hi\"\n"
        + "    optional slot min expects integer = 1\n\n"
        + "    let lit = off\n\n";

    readonly Values _store = new();

    /// <summary>A program whose one sequence holds <paramref name="step" /> on its own line.</summary>
    static string Program(string step) => Version + Head + "    sequence 1 {\n        " + step + "\n    }\n}\n";

    string Load(string text, Automation program) => ProgramReader.Load(text, program, _store);

    /// <summary>What <paramref name="step" /> is written back as once it is read.</summary>
    string Written(string step)
    {
        var program = new Automation();
        Assert.Equal("", Load(Program(step), program));
        var saved = ProgramWriter.Save(program, _store);
        var lines = saved.Split('\n');
        var at = Array.IndexOf(lines, "    sequence 1 {");
        return lines[at + 1].Trim();
    }

    /// <summary>A step that is already written as the writer writes it reads back as the same text, and again.</summary>
    [Theory]
    [InlineData("set Radius on [ pad ] to sum Radius on [ tanks ] * 10 + setting(Margin)")]
    [InlineData("set Radius on [ pad ] to 1 + 2 * 3")]
    [InlineData("set Radius on [ pad ] to (1 + 2) * 3")]
    [InlineData("set Radius on [ pad ] to 1 - (2 - 3)")]
    [InlineData("set Radius on [ pad ] to 1 - 2 - 3")]
    [InlineData("set Radius on [ pad ] to 8 / (2 / 2)")]
    [InlineData("set Radius on [ pad ] to 8 / 2 / 2")]
    [InlineData("set Radius on [ pad ] to -(1 + 2)")]
    [InlineData("set Radius on [ pad ] to 4 * -2")]
    [InlineData("set Radius on [ pad ] to 4 - -2")]
    [InlineData("set Radius on [ pad ] to min(1, 2, 3) + max(1.5, 2)")]
    [InlineData("set Radius on [ pad ] to round(2.5) * abs(-3)")]
    [InlineData("set Radius on [ pad ] to average Radius on [ tanks ]")]
    [InlineData("set Radius on [ pad ] to min Radius on [ tanks ]")]
    [InlineData("set Radius on [ pad ] to max Radius on [ tanks ] / 2")]
    [InlineData("set Radius on [ pad ] to Radius on [ tanks ]")]
    [InlineData("set Radius on [ pad ] to 1 + setting(Margin) * setting(min)")]
    [InlineData("call sprocket.label(\"Pressure: \" + average Radius on [ tanks ]) on [ pad ]")]
    [InlineData("call sprocket.label(\"a\" + \"b\", 1 + 2) on [ pad ]")]
    [InlineData("call sprocket.label(\"Level \" + sum Radius(1 + 2) on [ tanks ]) on [ pad ]")]
    [InlineData("call sprocket.label(\"Now \" + lit) on [ pad ]")]
    [InlineData("wait setting(Delay) * 2")]
    [InlineData("wait 2s + 30t")]
    [InlineData("wait 1s - 30t / 2")]
    [InlineData("wait round(sum Radius on [ tanks ]) * 1s")]
    public void AnExpressionIsWrittenBackAsItIsRead(string step)
    {
        Assert.Equal(step, Written(step));
        Assert.Equal(step, Written(Written(step)));
    }

    /// <summary>Parentheses a precedence does not need are not written back, and the ones it does are kept.</summary>
    [Theory]
    [InlineData("set Radius on [ pad ] to (sum Radius on [ tanks ] * 10) + setting(Margin)", "set Radius on [ pad ] to sum Radius on [ tanks ] * 10 + setting(Margin)")]
    [InlineData("set Radius on [ pad ] to ((1 + 2))", "set Radius on [ pad ] to 1 + 2")]
    [InlineData("set Radius on [ pad ] to (1 * 2) + 3", "set Radius on [ pad ] to 1 * 2 + 3")]
    [InlineData("set Radius on [ pad ] to (1 - 2) - 3", "set Radius on [ pad ] to 1 - 2 - 3")]
    [InlineData("set Radius on [ pad ] to 1 + (2 * 3)", "set Radius on [ pad ] to 1 + 2 * 3")]
    [InlineData("set Radius on [ pad ] to 2 * (1 + 3)", "set Radius on [ pad ] to 2 * (1 + 3)")]
    [InlineData("set Radius on [ pad ] to - 5", "set Radius on [ pad ] to -5")]
    [InlineData("set Radius on [ pad ] to (4)", "set Radius on [ pad ] to 4")]
    public void OnlyTheParenthesesPrecedenceNeedsAreWritten(string given, string written)
    {
        Assert.Equal(written, Written(given));
    }

    /// <summary>A plain literal, and a vector, read and write exactly as they did before expressions.</summary>
    [Theory]
    [InlineData("set Radius on [ pad ] to 4")]
    [InlineData("set Radius on [ pad ] to -4")]
    [InlineData("set Radius on [ pad ] to -4.5")]
    [InlineData("set Radius on [ pad ] to setting(Margin)")]
    [InlineData("wait 60t")]
    [InlineData("wait 1.5s")]
    [InlineData("wait Delay")]
    public void APlainLiteralIsNoExpression(string step)
    {
        var program = new Automation();
        Assert.Equal("", Load(Program(step), program));
        Assert.Equal(step, Written(step));

        var act = program.Systems[0].Sequences[0].Steps[0] as Act;
        if (act != null)
            Assert.False(act.Arguments[0].IsExpression);
    }

    /// <summary>A dash written against its number is the number's sign, so a vector with a negative number reads as it always did.</summary>
    [Fact]
    public void ADashAgainstItsNumberIsTheNumbersSignAndAVectorIsStillAVector()
    {
        const string text = Version + "let door1 = block 'Door/LargeBlockDoor' [ 74857693847562934, [ (0 2 -1), (3 0 0) ], \"Hangar Door\" ]\n";
        var program = new Automation();
        Assert.Equal("", Load(text, program));
        Assert.Equal(new[] { new VRageMath.Vector3I(0, 2, -1), new VRageMath.Vector3I(3, 0, 0) }, program.FindRef("door1")!.Keys[1].Cells);
    }

    /// <summary>A reading, written with a combining word or without, is an expression of its own with the combination it says.</summary>
    [Theory]
    [InlineData("sum", Combination.Sum)]
    [InlineData("average", Combination.Average)]
    [InlineData("min", Combination.Min)]
    [InlineData("max", Combination.Max)]
    public void ACombiningWordMakesTheReadingCombineItsBlocks(string word, Combination combine)
    {
        var program = new Automation();
        Assert.Equal("", Load(Program($"set Radius on [ pad ] to {word} Radius on [ tanks ]"), program));

        var act = (Act)program.Systems[0].Sequences[0].Steps[0];
        var reading = Assert.IsType<BlockReading>(act.Arguments[0].Tree);
        Assert.Equal(combine, reading.Combine);
        Assert.Equal("Radius", reading.Key.Id);
        Assert.Equal(new[] { "tanks" }, reading.Targets);
    }

    /// <summary>The tree an expression is read as binds as the language says: products first, and left to right.</summary>
    [Fact]
    public void TheTreeFollowsPrecedence()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("set Radius on [ pad ] to 1 + 2 * 3 - 4"), program));

        var act = (Act)program.Systems[0].Sequences[0].Steps[0];
        var minus = Assert.IsType<Arithmetic>(act.Arguments[0].Tree);
        Assert.Equal(Operator.Subtract, minus.Op);

        var plus = Assert.IsType<Arithmetic>(minus.Left);
        Assert.Equal(Operator.Add, plus.Op);
        Assert.Equal(Operator.Multiply, Assert.IsType<Arithmetic>(plus.Right).Op);
    }

    /// <summary>A reading of several blocks needs a combining word, and a function needs the arguments it takes.</summary>
    [Theory]
    [InlineData("set Radius on [ pad ] to Radius on [ pad, tanks ]")]
    [InlineData("set Radius on [ pad ] to min(1)")]
    [InlineData("set Radius on [ pad ] to round(1, 2)")]
    [InlineData("set Radius on [ pad ] to abs()")]
    [InlineData("set Radius on [ pad ] to 1 +")]
    [InlineData("set Radius on [ pad ] to (1 + 2")]
    [InlineData("set Radius on [ pad ] to 1 + 2)")]
    [InlineData("set Radius on [ pad ] to sum 4")]
    [InlineData("set Radius on [ pad ] to 1 + [ pad ]")]
    public void ATroubledExpressionIsTroubleWhereItStands(string step)
    {
        var program = new Automation();
        var trouble = Load(Program(step), program);

        Assert.NotEqual("", trouble);
        Assert.Contains("Line ", trouble);
    }

    /// <summary>A watcher, a clock and a watcher's look interval take expressions as a step does, and are written back as they were.</summary>
    [Theory]
    [InlineData("when all Radius on [ tanks ] > 1 + 2 then run 1")]
    [InlineData("when all Radius on [ tanks ] > sum Radius on [ pad ] * 2 then run 1")]
    [InlineData("when min Radius on [ tanks ] <= 5 - Margin then run 1")]
    [InlineData("when max Radius on [ tanks ] >= Margin + 1 then run 1")]
    [InlineData("when all Radius(1 + 2) on [ tanks ] > 1 then run 1")]
    [InlineData("when all Radius on [ tanks ] > 1 every Delay * 2 then run 1")]
    [InlineData("every Delay * 2 run 1")]
    [InlineData("every 2s + 30t run 1")]
    public void AWatcherAndAClockTakeExpressions(string trigger)
    {
        var program = new Automation();
        Assert.Equal("", Load(Version + Head + "    " + trigger + "\n\n    sequence 1 {\n        wait 1s\n    }\n}\n", program));

        Assert.Contains("    " + trigger + "\n", ProgramWriter.Save(program, _store));
    }

    /// <summary>An if takes <c>min</c> and <c>max</c> as it takes any, all, sum and average, and a state named so before a member is still a state.</summary>
    [Theory]
    [InlineData("if min Radius on [ tanks ] > 1 then {\n            wait 1s\n        }")]
    [InlineData("if max Radius on [ tanks ] <= 4 then {\n            wait 1s\n        }")]
    public void AnIfTakesMinAndMaxAsQuantifiers(string step)
    {
        var program = new Automation();
        Assert.Equal("", Load(Program(step), program));

        Assert.Contains(step.Split('\n')[0], ProgramWriter.Save(program, _store));
    }

    /// <summary>What an if compares with can be an expression, read back as one.</summary>
    [Fact]
    public void WhatAnIfComparesWithCanBeAnExpression()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("if all Radius on [ tanks ] > Margin * 2 then {\n            wait 1s\n        }"), program));

        var test = ((If)program.Systems[0].Sequences[0].Steps[0]).Compared!;
        Assert.True(test.Value.IsExpression);
        Assert.Contains("all Radius on [ tanks ] > Margin * 2 then", ProgramWriter.Save(program, _store));
    }

    /// <summary>A name spelled like a combining word or a function is a name still where it stands alone.</summary>
    [Fact]
    public void ASettingNamedLikeAFunctionIsStillASetting()
    {
        Assert.Equal("set Radius on [ pad ] to setting(min) * 2", Written("set Radius on [ pad ] to setting(min) * 2"));
        Assert.Equal("set Radius on [ pad ] to setting(min) * 2", Written("set Radius on [ pad ] to min * 2"));
    }

    /// <summary>A state an expression names is read as the state, and written back so.</summary>
    [Fact]
    public void AStateIsAnOperand()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("call sprocket.label(\"Light \" + lit) on [ pad ]"), program));

        var act = (Act)program.Systems[0].Sequences[0].Steps[0];
        var join = Assert.IsType<Arithmetic>(act.Arguments[0].Tree);
        Assert.IsType<StateOperand>(join.Right);
    }

    /// <summary>A program written for the draft before expressions reads as it did, and is written in the current one.</summary>
    [Fact]
    public void AProgramOfTheDraftBeforeExpressionsIsUpgradedToTheCurrentOne()
    {
        var program = new Automation();
        var text = Program("set Radius on [ pad ] to 4").Replace("sprocket 2 alpha 4", "sprocket 2 alpha 3");

        Assert.Equal("", Load(text, program));
        Assert.StartsWith("sprocket 2 alpha 4\n", ProgramWriter.Save(program, _store));
    }

    /// <summary>The draft the build writes is the one with expressions, so an older build refuses what holds one.</summary>
    [Fact]
    public void TheCurrentDraftIsTheOneWithExpressions()
    {
        Assert.Equal(4, ScriptFormat.Draft);
    }

    /// <summary>An attribute takes an expression as it takes a literal, and a program whose statement has one is written back as it was.</summary>
    [Fact]
    public void AnAttributeMayHoldAnExpression()
    {
        var program = new Automation();
        Assert.Equal("", Load(Version + Head + "    #@ count: 1 + 2\n    sequence 1 {\n        wait 1s\n    }\n}\n", program));

        Assert.Contains("    #@ count: 1 + 2\n    sequence 1 {", ProgramWriter.Save(program, _store));
    }

    /// <summary>A state named like a quantifier is a state still where no member follows it, and is quoted where a test starts with it.</summary>
    [Fact]
    public void AStateNamedMaxIsStillAStateInATest()
    {
        var program = new Automation();
        var text = Version + "system \"" + Identity + "\" as Airlock\n{\n    let max = off\n\n    sequence 1 {\n        if max == on then {\n            wait 1s\n        }\n    }\n}\n";
        Assert.Equal("", Load(text, program));

        Assert.Contains("if 'max' == on then {", ProgramWriter.Save(program, _store));
    }

    /// <summary>An abort's message and what a state is set to take expressions, and are written back as they were.</summary>
    [Theory]
    [InlineData("abort saying \"Level \" + sum Radius on [ tanks ] as warning")]
    [InlineData("abort saying greeting + \"!\"")]
    public void AnAbortAndASetStateTakeExpressions(string step)
    {
        var program = new Automation();
        Assert.Equal("", Load(Program(step), program));
        Assert.Equal(step, Written(step));
    }

    /// <summary>A state is set to an expression of a flag, which reads back as it was written.</summary>
    [Fact]
    public void AStateIsSetToAnExpression()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("set lit to Radius on [ pad ]"), program));

        var set = (SetState)program.Systems[0].Sequences[0].Steps[0];
        Assert.NotNull(set.Expression);
        Assert.Equal("set lit to Radius on [ pad ]", Written("set lit to Radius on [ pad ]"));
    }

    /// <summary>Sprocket takes no expression in a constant, and says plainly that this is its limit as a host and not the language's.</summary>
    [Theory]
    [InlineData("    let wide = 1 + 2\n", "")]
    [InlineData("    optional slot Extra expects real = 1 + 2\n", "")]
    [InlineData("", "    Margin = 1 + 2\n")]
    public void AConstantTakesNoExpression(string inSystem, string inInstance)
    {
        var program = new Automation();
        var text = Version + Head + inSystem + "    sequence 1 {\n        wait 1s\n    }\n}\n\ninstance 1 of \"" + Identity + "\" {\n" + inInstance + "}\n";
        var trouble = Load(text, program);

        Assert.StartsWith("Line ", trouble);
        Assert.EndsWith(": Sprocket does not take an expression in a default: a state's starting value, a setting's default, or an installation's answer. This is a limit of Sprocket as a host, not of the language. Write a literal here.", trouble);
    }

    /// <summary>A name spelled like the new words, in a program written before them, is a name still.</summary>
    [Theory]
    [InlineData("sprocket 2 alpha 1")]
    [InlineData("sprocket 1 alpha 20")]
    [InlineData("sprocket 2 alpha 3")]
    public void AnOlderProgramNamingThingsMinAndMaxStillReads(string version)
    {
        var program = new Automation();
        Assert.Equal("", Load(version + "\n\nlet min = block 'Door/LargeBlockDoor' [ 5 ]\nlet max = block 'Door/LargeBlockDoor' [ 6 ]\n", program));
        Assert.NotNull(program.FindRef("min"));
        Assert.NotNull(program.FindRef("max"));
    }

    /// <summary>A shared text of a program holding expressions is coded and decoded as every program is, and reads back as it was.</summary>
    [Theory]
    [InlineData("set Radius on [ pad ] to sum Radius on [ tanks ] * 10 + setting(Margin)")]
    [InlineData("set Radius on [ pad ] to -(1 - 2) / max(3, 4.5)")]
    [InlineData("call sprocket.label(\"a \" + average Radius(1 + 2) on [ tanks ]) on [ pad ]")]
    [InlineData("wait 2s - 30t")]
    public void AnExpressionSurvivesTheSharedTextsCoding(string step)
    {
        var coded = TokenCode.Decode(
            TokenCode.Encode(Program(step)))!;

        var program = new Automation();
        Assert.Equal("", Load(coded, program));
        var lines = ProgramWriter.Save(program, _store).Split('\n');
        Assert.Contains("        " + step, lines);
    }

    /// <summary>A copy of a program holds the expressions as it does, with names of its own to rename.</summary>
    [Fact]
    public void ACopyHoldsTheExpressionsAndRenamingASlotReachesThem()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("set Radius on [ pad ] to sum Radius on [ tanks ] * 10 + Margin"), program));

        var into = new Values();
        var copy = new Automation();
        ProgramCopy.Into(program, _store, copy, into);

        var system = copy.Systems[0];
        system.RenamePart(system.FindPart("tanks")!, "cisterns");
        Assert.Contains("sum Radius on [ cisterns ] * 10 + Margin", ProgramWriter.Save(copy, into));
        Assert.Contains("sum Radius on [ tanks ] * 10 + Margin", ProgramWriter.Save(program, _store));
    }

    /// <summary>Renaming a setting reaches the expressions that name it.</summary>
    [Fact]
    public void RenamingASettingReachesTheExpressionsNamingIt()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("set Radius on [ pad ] to 1 + Margin * setting(Margin)"), program));

        var system = program.Systems[0];
        system.RenamePart(system.FindPart("Margin")!, "Slack");

        Assert.Contains("set Radius on [ pad ] to 1 + Slack * setting(Slack)", ProgramWriter.Save(program, _store));
    }

    /// <summary>Renaming a state reaches the expressions that name it.</summary>
    [Fact]
    public void RenamingAStateReachesTheExpressionsNamingIt()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("call sprocket.label(\"Light \" + lit) on [ pad ]"), program));

        StateMentions.RenameState(program.Systems[0], "lit", "glowing");

        Assert.Contains("\"Light \" + glowing", ProgramWriter.Save(program, _store));
    }
}
