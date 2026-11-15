using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Sequences of a system that name the same <c>exclusive</c> id run one at a time in each instance: a start while another of
/// them runs is missed, as a start while the sequence itself runs is, and the missed one knows which held it.
/// </summary>
public class ExclusiveTests
{
    const string Version = "sprocket 1 alpha 27\n\n";

    const string Drills = Version
        + "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as Drill\n{\n"
        + "    #@ category: \"Drilling\"\n"
        + "    sequence 1 as Shallow exclusive Drilling {\n        wait 10s\n    }\n\n"
        + "    #@ category: \"Drilling\"\n"
        + "    sequence 2 as Deep exclusive Drilling {\n        wait 10s\n    }\n\n"
        + "    sequence 3 as Stow {\n        wait 10s\n    }\n"
        + "}\n";

    static readonly Values Store = new Values();

    static (Automation Program, AutomationRunner Runner) Running(string text)
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var runner = new AutomationRunner(program, new Rigged().World);
        runner.Tick(1);
        return (program, runner);
    }

    /// <summary>The id is read after the name, written back there, and copied with the sequence.</summary>
    [Fact]
    public void TheIdIsReadAndWrittenBack()
    {
        var (program, _) = Running(Drills);
        var system = program.Systems[0];
        Assert.Equal("Drilling", system.FindSequence(1)!.Exclusive);
        Assert.Equal("", system.FindSequence(3)!.Exclusive);

        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("    sequence 1 as Shallow exclusive Drilling {\n", written);
        Assert.Contains("    sequence 3 as Stow {\n", written);

        var copy = new Automation();
        ProgramCopy.Into(program, Store, copy, new Values());
        Assert.Equal("Drilling", copy.Systems[0].FindSequence(2)!.Exclusive);
    }

    /// <summary>An id that needs quoting is written quoted, and read back as it was.</summary>
    [Fact]
    public void AnIdWithASpaceIsQuoted()
    {
        var (program, _) = Running(Drills.Replace("exclusive Drilling", "exclusive 'Deep Work'"));
        Assert.Equal("Deep Work", program.Systems[0].FindSequence(1)!.Exclusive);
        Assert.Contains("    sequence 1 as Shallow exclusive 'Deep Work' {\n", ProgramWriter.Save(program, Store));
    }

    /// <summary>An exclusive with no id after it is an error that says what goes there.</summary>
    [Fact]
    public void AnExclusiveWithNoIdIsAnError()
    {
        var program = new Automation();
        var said = ProgramReader.Load(Drills.Replace("as Stow {", "as Stow exclusive {"), program, Store);
        Assert.Contains("after 'exclusive'", said);
    }

    /// <summary>A start while another sequence with the same id runs is missed, and says which one held it; one with no id starts.</summary>
    [Fact]
    public void AStartWhileAnotherHolderRunsIsMissed()
    {
        var (program, runner) = Running(Drills);
        var system = program.Systems[0];
        var self = system.Uses[0];
        var shallow = system.FindSequence(1)!;
        var deep = system.FindSequence(2)!;

        runner.Call(shallow, self);
        runner.Call(deep, self);
        runner.Call(system.FindSequence(3)!, self);

        var held = runner.Get(deep, self)!;
        Assert.False(held.IsRunning);
        Assert.Equal(1, held.SkippedCount);
        Assert.Same(shallow, held.HeldBy);
        Assert.True(runner.Get(system.FindSequence(3)!, self)!.IsRunning);
    }

    /// <summary>Once the holder has ended, the other starts, and no longer says it was held.</summary>
    [Fact]
    public void AStartAfterTheHolderEndsGoesAhead()
    {
        var (program, runner) = Running(Drills);
        var system = program.Systems[0];
        var self = system.Uses[0];
        var shallow = system.FindSequence(1)!;
        var deep = system.FindSequence(2)!;

        runner.Call(shallow, self);
        runner.Call(deep, self);
        runner.Get(shallow, self)!.Stop();
        runner.Call(deep, self);

        var run = runner.Get(deep, self)!;
        Assert.True(run.IsRunning);
        Assert.Null(run.HeldBy);
    }

    /// <summary>Each installation runs its own one at a time: a holder running in one does not hold the other back.</summary>
    [Fact]
    public void EachInstallationHoldsApartOnItsOwn()
    {
        var text = Drills.Replace("system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as Drill", "system blueprint \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Drill")
            + "\ninstance 1 of MalTek.Drill {\n}\n\ninstance 2 of MalTek.Drill {\n}\n";
        var (program, runner) = Running(text);
        var system = program.Systems[0];
        var first = system.Instances[0];
        var second = system.Instances[1];

        runner.Call(system.FindSequence(1)!, first);
        runner.Call(system.FindSequence(2)!, second);

        Assert.True(runner.Get(system.FindSequence(2)!, second)!.IsRunning);
    }
}
