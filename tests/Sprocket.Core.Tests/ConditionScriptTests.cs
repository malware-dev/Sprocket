using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Conditions in a program's text: an if and a when test any expression that comes to a flag, joined with and, or and not,
/// while a test written as the statement's parts reads as it always has.
/// </summary>
public class ConditionScriptTests
{
    const string Identity = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    const string Version = "sprocket 3 alpha 3\n\n";

    const string Head = "system blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n"
        + "    required slot pad expects InteriorLight\n"
        + "    required slot tanks expects InteriorLight\n"
        + "    optional slot Margin expects real = 2\n\n"
        + "    let lit = off\n"
        + "    let count = 0\n\n";

    readonly Values _store = new();

    static string Program(string step) => Version + Head + "    sequence 1 {\n        " + step + "\n    }\n}\n";

    static string Watching(string trigger) => Version + Head + "    " + trigger + "\n\n    sequence 1 {\n        wait 1s\n    }\n}\n";

    string Load(string text, Automation program) => ProgramReader.Load(text, program, _store);

    /// <summary>The first line of the if <paramref name="condition" /> tests, read and written back.</summary>
    string Tested(string condition, out BranchTest test)
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("if " + condition + " then {\n            wait 1s\n        }"), program));
        test = ((If)program.Systems[0].Sequences[0].Steps[0]).Test;
        var lines = ProgramWriter.Save(program, _store).Split('\n');
        return lines[Array.IndexOf(lines, "    sequence 1 {") + 1].Trim();
    }

    /// <summary>An if testing an expression reads it as one, and writes it back as it was.</summary>
    [Theory]
    [InlineData("all Radius on [ tanks ] > 2 and not lit")]
    [InlineData("lit or Radius on [ pad ] < setting(Margin)")]
    [InlineData("not lit")]
    [InlineData("lit")]
    [InlineData("5 < Radius on [ pad ]")]
    [InlineData("count + 1 > 3")]
    [InlineData("any Radius on [ tanks ] == 2 or all Radius on [ tanks ] >= 1")]
    [InlineData("(lit or lit) and lit")]
    [InlineData("not (any Radius on [ tanks ] == 2)")]
    [InlineData("count >= 1 and count <= 4")]
    public void AnIfTestsAnExpression(string condition)
    {
        BranchTest test;
        Assert.Equal("if " + condition + " then {", Tested(condition, out test));
        Assert.IsType<ExpressionTest>(test);
    }

    /// <summary>A test written as the statement's parts reads as the test it always was, though it is also an expression.</summary>
    [Theory]
    [InlineData("all Radius on [ tanks ] > 2", typeof(Comparison))]
    [InlineData("sum Radius on [ tanks ] >= Margin * 2", typeof(Comparison))]
    [InlineData("lit == on", typeof(StateTest))]
    [InlineData("count > 2", typeof(StateTest))]
    public void ATestWrittenAsPartsReadsAsItsOwnTest(string condition, Type kind)
    {
        BranchTest test;
        Assert.Equal("if " + condition + " then {", Tested(condition, out test));
        Assert.IsType(kind, test);
    }

    /// <summary>An expression that would read as a test written as parts is written in parentheses, so it reads back as the expression it is.</summary>
    [Fact]
    public void AnExpressionShapedLikeATestIsWrittenInParentheses()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("if (count == 3) then {\n            wait 1s\n        }"), program));
        var step = (If)program.Systems[0].Sequences[0].Steps[0];
        Assert.IsType<ExpressionTest>(step.Test);
        Assert.Contains("if (count == 3) then {", ProgramWriter.Save(program, _store));
    }

    /// <summary>A comparison binds looser than arithmetic and tighter than and, and and tighter than or.</summary>
    [Fact]
    public void TheConditionFollowsPrecedence()
    {
        BranchTest test;
        Tested("lit or count + 1 > 2 and not lit", out test);
        var or = Assert.IsType<Logical>(((ExpressionTest)test).Condition);
        Assert.Equal(Connective.Or, or.Joins);
        var and = Assert.IsType<Logical>(or.Right);
        Assert.Equal(Connective.And, and.Joins);
        var compared = Assert.IsType<Comparing>(and.Left);
        Assert.IsType<Arithmetic>(compared.Left);
        Assert.IsType<Inversion>(and.Right);
    }

    /// <summary>A state is set to a comparison, which is a flag.</summary>
    [Fact]
    public void AStateIsSetToAComparison()
    {
        var program = new Automation();
        Assert.Equal("", Load(Program("set lit to Radius on [ pad ] > 2 and count != 0"), program));
        Assert.Contains("set lit to Radius on [ pad ] > 2 and count != 0", ProgramWriter.Save(program, _store));
    }

    /// <summary>A when on an expression reads it as one, with or without how often it looks, and writes it back as it was.</summary>
    [Theory]
    [InlineData("when average Radius on [ tanks ] < setting(Margin) or lit == on then run 1")]
    [InlineData("when all Radius on [ tanks ] > 2 and not lit every 30t then run 1")]
    [InlineData("when count >= 3 and lit then run 1")]
    [InlineData("when Radius on [ pad ] > 4 then run 1")]
    public void AWhenWatchesAnExpression(string trigger)
    {
        var program = new Automation();
        Assert.Equal("", Load(Watching(trigger), program));
        Assert.IsType<ExpressionTrigger>(program.Systems[0].Triggers[0]);
        Assert.Contains("    " + trigger + "\n", ProgramWriter.Save(program, _store));
    }

    /// <summary>A when whose whole expression is an any fires for each block, as a watcher written as parts does.</summary>
    [Fact]
    public void AWholeAnyIsAWatcherOfEachBlock()
    {
        var program = new Automation();
        Assert.Equal("", Load(Watching("when (any Radius on [ tanks ] > 2) then run 1"), program));
        var when = Assert.IsType<WhenTrigger>(program.Systems[0].Triggers[0]);
        Assert.Equal(Quantifier.Any, when.Quantifier);
        Assert.Contains("    when any Radius on [ tanks ] > 2 then run 1\n", ProgramWriter.Save(program, _store));
    }

    /// <summary>A condition the language does not allow is trouble where it is written.</summary>
    [Theory]
    [InlineData("if count < 2 < 3 then {\n        }")]
    [InlineData("if lit and then {\n        }")]
    [InlineData("if 5 then {\n        }")]
    [InlineData("if not then {\n        }")]
    public void AWrongConditionIsTrouble(string step)
    {
        Assert.NotEqual("", Load(Program(step), new Automation()));
    }

    /// <summary>A when that reads nothing that can change is trouble, and so is an every on one that reads no block.</summary>
    [Theory]
    [InlineData("when setting(Margin) > 2 and on then run 1")]
    [InlineData("when count > 2 and lit every 30t then run 1")]
    public void AWhenWithNothingToWatchIsTrouble(string trigger)
    {
        Assert.NotEqual("", Load(Watching(trigger), new Automation()));
    }

    /// <summary>A name spelled and, or or not where it stands as a value is a name still.</summary>
    [Fact]
    public void ANameSpelledLikeAJoiningWordIsAName()
    {
        var program = new Automation();
        var text = Version + Head.Replace("    let count = 0\n", "    let count = 0\n    let or = off\n") + "    sequence 1 {\n        set lit to or\n    }\n}\n";
        Assert.Equal("", Load(text, program));
        Assert.Contains("set lit to or", ProgramWriter.Save(program, _store));
    }
}
