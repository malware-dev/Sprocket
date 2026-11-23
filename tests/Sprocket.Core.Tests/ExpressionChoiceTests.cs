using Sprocket.Host.SpaceEngineers.Features.States.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A choice of a logic system's type named inside an expression is renamed with the choice, and counts as using it.</summary>
public class ExpressionChoiceTests
{
    const string Program = "sprocket 3 alpha 3\n\n"
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
        + "    optional slot Home expects side = outside\n\n"
        + "    type side = inside | outside\n\n"
        + "    let where = outside\n"
        + "    let lit = off\n\n"
        + "    when where == inside or lit then run 1\n\n"
        + "    sequence 1 {\n"
        + "        if where != inside and not lit then {\n"
        + "            set lit to setting(Home) == inside\n"
        + "        }\n"
        + "        set where to inside\n"
        + "    }\n}\n";

    readonly Values _store = new();

    AutomationSystem Read(string text)
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, _store));
        return program.Systems[0];
    }

    string Written(AutomationSystem system) => ProgramWriter.Blueprint(system);

    /// <summary>Renaming a choice renames it where an if, a when and a set step compare a state or a setting with it, and undoing it renames it back.</summary>
    [Fact]
    public void RenamingAChoiceReachesItInsideExpressions()
    {
        var system = Read(Program);
        var rename = new RenameChoice(system.FindType("side")!, "inside", "indoors");

        rename.Apply(system);
        var renamed = Written(system);
        Assert.Contains("when where == indoors or lit then run 1", renamed);
        Assert.Contains("if where != indoors and not lit then {", renamed);
        Assert.Contains("set lit to setting(Home) == indoors", renamed);
        Assert.Contains("set where to indoors", renamed);
        Assert.DoesNotContain("inside", renamed);
        Read(renamed);

        rename.Undo(system);
        Assert.Contains("if where != inside and not lit then {", Written(system));
    }

    /// <summary>A name that is no choice of the type, compared with something else, is left as it is.</summary>
    [Fact]
    public void ANameComparedWithAnotherTypeIsNotRenamed()
    {
        var system = Read(Program.Replace("    let lit = off\n", "    let lit = off\n    let label = \"inside\"\n")
            .Replace("set where to inside\n", "set lit to label == \"inside\"\n"));
        new RenameChoice(system.FindType("side")!, "inside", "indoors").Apply(system);

        Assert.Contains("set lit to label == \"inside\"", Written(system));
    }

    /// <summary>A choice an expression alone names is in use, so the editor does not treat it as free to remove.</summary>
    [Fact]
    public void AChoiceAnExpressionNamesIsInUse()
    {
        var system = Read(Program.Replace("        set where to inside\n", "")
            .Replace("    when where == inside or lit then run 1\n\n", "")
            .Replace("            set lit to setting(Home) == inside\n", ""));

        Assert.True(StateMentions.Mentions(system, "side", "inside"));
        Assert.True(StateMentions.Mentions(system, "lit"));
    }

    /// <summary>A choice nothing names is free.</summary>
    [Fact]
    public void AChoiceNothingNamesIsFree()
    {
        var system = Read(Program.Replace("        set where to inside\n", "")
            .Replace("    when where == inside or lit then run 1\n\n", "")
            .Replace("            set lit to setting(Home) == inside\n", "")
            .Replace("if where != inside and not lit then", "if not lit then"));

        Assert.False(StateMentions.Mentions(system, "side", "inside"));
    }
}
