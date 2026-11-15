using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A sequence's <c>catch</c> runs where its run fails, and its <c>finally</c> however the run ends, after the catch, switching
/// off included; a stop ends a finally at once.
/// </summary>
public class HandlerTests
{
    const string Head = "sprocket 1 alpha 27\n\n"
        + "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as Drill\n{\n"
        + "    let caught = off\n"
        + "    let cleaned = off\n\n"
        + "    sequence 1 as Work {\n"
        + "        wait 10t\n";

    const string Handlers = "        catch {\n"
        + "            set caught to on\n"
        + "        }\n"
        + "        finally {\n"
        + "            wait 10t\n"
        + "            set cleaned to on\n"
        + "        }\n"
        + "    }\n"
        + "}\n";

    static readonly Values Store = new Values();

    /// <summary>The program with <paramref name="body" /> after the sequence's first wait, run from its start for <paramref name="ticks" /> ticks.</summary>
    static (Automation Program, AutomationRunner Runner, SequenceRunner Run) Ran(string body, int ticks)
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Head + body + Handlers, program, Store));
        var runner = new AutomationRunner(program, new Rigged().World);
        runner.Tick(1);
        var system = program.Systems[0];
        runner.Call(system.FindSequence(1)!, system.Uses[0]);
        Tick(runner, 2, ticks);
        return (program, runner, runner.Get(system.FindSequence(1)!, system.Uses[0])!);
    }

    static void Tick(AutomationRunner runner, int from, int to)
    {
        for (var tick = from; tick <= to; tick++)
            runner.Tick(tick);
    }

    static bool Flag(Automation program, AutomationRunner runner, string state)
    {
        var system = program.Systems[0];
        return runner.States.Read(system, system.Uses[0], system.FindState(state)!).Said == Literal.Of(true).Said;
    }

    /// <summary>The handlers are read in place, written back as they were, and their order and place are checked.</summary>
    [Fact]
    public void TheHandlersAreReadAndWrittenBack()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Head + Handlers, program, Store));
        var steps = program.Systems[0].FindSequence(1)!.Steps;
        Assert.IsType<Catch>(steps[1]);
        Assert.IsType<Finally>(steps[3]);

        Assert.Contains("        wait 10t\n        catch {\n            set caught to on\n        }\n        finally {\n            wait 10t\n", ProgramWriter.Save(program, Store));
    }

    /// <summary>A catch or finally out of its place is an error that says where it goes.</summary>
    [Theory]
    [InlineData("        if caught == on then {\n            catch {\n            }\n        }\n", "never inside an if")]
    [InlineData("        finally {\n        }\n", "one catch and one finally, the catch first")]
    public void AHandlerOutOfPlaceIsAnError(string body, string said)
    {
        var program = new Automation();
        Assert.Contains(said, ProgramReader.Load(Head + body + Handlers, program, Store));
    }

    /// <summary>A step after the handlers is an error.</summary>
    [Fact]
    public void AStepAfterTheHandlersIsAnError()
    {
        var program = new Automation();
        var text = Head + Handlers.Replace("        }\n    }\n}\n", "        }\n        wait 1t\n    }\n}\n");
        Assert.Contains("catch and finally come last", ProgramReader.Load(text, program, Store));
    }

    /// <summary>A run that goes well passes its catch by, and runs its finally.</summary>
    [Fact]
    public void ARunThatGoesWellRunsOnlyItsFinally()
    {
        var (program, runner, run) = Ran("", 40);
        Assert.False(run.IsRunning);
        Assert.False(Flag(program, runner, "caught"));
        Assert.True(Flag(program, runner, "cleaned"));
    }

    /// <summary>An abort as a warning fails the run: its catch runs, then its finally.</summary>
    [Fact]
    public void AWarningRunsTheCatchThenTheFinally()
    {
        var (program, runner, run) = Ran("        abort saying \"Jammed\" as warning\n", 40);
        Assert.False(run.IsRunning);
        Assert.True(Flag(program, runner, "caught"));
        Assert.True(Flag(program, runner, "cleaned"));
    }

    /// <summary>An abort as a note ends the run without failing it: the finally runs, the catch does not.</summary>
    [Fact]
    public void ANoteRunsOnlyTheFinally()
    {
        var (program, runner, _) = Ran("        abort\n", 40);
        Assert.False(Flag(program, runner, "caught"));
        Assert.True(Flag(program, runner, "cleaned"));
    }

    /// <summary>A problem switches the installation off, and the catch and finally still run to their end.</summary>
    [Fact]
    public void AProblemRunsTheHandlersSwitchedOff()
    {
        var (program, runner, run) = Ran("        abort saying \"Jammed\" as problem\n", 40);
        Assert.False(program.Systems[0].Uses[0].Enabled && program.Systems[0].Enabled);
        Assert.False(run.IsRunning);
        Assert.True(Flag(program, runner, "caught"));
        Assert.True(Flag(program, runner, "cleaned"));
    }

    /// <summary>A stop ends the run's own steps and runs its finally, which counts as running; a second stop ends the finally at once.</summary>
    [Fact]
    public void AStopRunsTheFinallyAndASecondStopEndsIt()
    {
        var (program, runner, run) = Ran("        wait 100t\n", 15);
        run.Stop();
        Assert.True(run.IsRunning);
        Assert.True(run.Cleaning);

        run.Stop();
        Assert.False(run.IsRunning);
        Assert.False(Flag(program, runner, "cleaned"));
        Assert.False(Flag(program, runner, "caught"));
    }

    /// <summary>Switching the logic system off ends the run's own steps, and its finally runs to its end while off.</summary>
    [Fact]
    public void SwitchingOffRunsTheFinally()
    {
        var (program, runner, run) = Ran("        wait 100t\n", 15);
        program.Systems[0].Enabled = false;

        Tick(runner, 16, 60);

        Assert.False(run.IsRunning);
        Assert.False(Flag(program, runner, "caught"));
        Assert.True(Flag(program, runner, "cleaned"));
    }

    /// <summary>A failure inside the catch is a fault as if there were no catch: the finally runs, and the catch does not run again.</summary>
    [Fact]
    public void AFailureInTheCatchGoesOnToTheFinally()
    {
        var program = new Automation();
        var text = Head + Handlers.Replace("            set caught to on\n", "            set caught to on\n            abort saying \"Again\" as warning\n            set caught to off\n");
        Assert.Equal("", ProgramReader.Load(text.Replace("        wait 10t\n        catch", "        wait 10t\n        abort as warning\n        catch"), program, Store));
        var runner = new AutomationRunner(program, new Rigged().World);
        runner.Tick(1);
        var system = program.Systems[0];
        runner.Call(system.FindSequence(1)!, system.Uses[0]);
        Tick(runner, 2, 60);

        Assert.True(Flag(program, runner, "caught"));
        Assert.True(Flag(program, runner, "cleaned"));
    }
}
