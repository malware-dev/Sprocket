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

    /// <summary>
    /// A run that fails into its catch and goes on into its finally, with the debug log on, logs both and finishes: the finally,
    /// reached as the next step, by the line that opens it.
    /// </summary>
    [Fact]
    public void TheDebugLogNamesTheHandlersItRuns()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Head + "        abort saying \"Stuck\" as warning\n" + Handlers, program, Store));
        var runner = new AutomationRunner(program, new Rigged().World);
        var lines = new List<string>();
        runner.Log = new RunLog(lines.Add, () => "Drill Rig");
        runner.Tick(1);
        var system = program.Systems[0];
        runner.Call(system.FindSequence(1)!, system.Uses[0]);
        Tick(runner, 2, 40);

        Assert.Contains(lines, line => line.EndsWith("step 3 catch"));
        Assert.Contains(lines, line => line.EndsWith("step 5: finally {"));
        Assert.Contains(lines, line => line.EndsWith("finished"));
    }

    /// <summary>
    /// The hangar gate reported in issue 105, its script as the player wrote it, runs with the debug log on: the opening gate's
    /// sequence goes on into its catch as its next step, which the log names by the line that opens it.
    /// </summary>
    [Fact]
    public void TheReportedHangarGateRunsWithTheDebugLogOn()
    {
        var rigged = new Rigged();
        rigged.Door(101, "Hangar Gate");
        rigged.Vent(102, "Hangar Vent");
        rigged.Light(103, "Hangar Light");
        rigged.Panel(104, "Hangar Button");
        rigged.Rig.TextPanel(105, "Hangar Screen");
        var text = "sprocket 2\n\n"
        + "let gate1 = block 'Door/LargeBlockDoor' [ 101, \"Hangar Gate\" ]\n"
        + "let vent1 = block 'AirVent/SmallAirVent' [ 102, \"Hangar Vent\" ]\n"
        + "let light1 = block 'InteriorLight/LargeBlockLight_1corner' [ 103, \"Hangar Light\" ]\n"
        + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 104, \"Hangar Button\" ]\n"
        + "let screen1 = block 'TextPanel/LargeTextPanel' [ 105, \"Hangar Screen\" ]\n\n"
        + "system blueprint \"5e1c7a90-2b3d-4f6e-8a9c-1d2e3f4a5b6c\" as Reported.Hangar\n{\n"
        + "    required slot 'signal slot 1' expects signal\n"
        + "    required slot 'hangar vent' expects AirVent\n"
        + "    required slot gate expects Door\n"
        + "    required slot light expects LightingBlock\n"
        + "    required slot screen expects sprocket.screen\n"
        + "\n"
        + "    type door = opening | closing | open | close\n"
        + "\n"
        + "    let hangar = close\n"
        + "\n"
        + "    whenever 'signal slot 1' then run 1\n"
        + "\n"
        + "    when hangar == opening then run 2\n"
        + "\n"
        + "    when hangar == closing then run 3\n"
        + "\n"
        + "    when hangar == close then run 4\n"
        + "\n"
        + "    when hangar == open then run 4\n"
        + "\n"
        + "    sequence 1 as 'toggle gate' {\n"
        + "        if hangar == open then {\n"
        + "            set hangar to closing\n"
        + "            call sprocket.show_text(\"closing\") on [ screen ]\n"
        + "        }\n"
        + "        else if hangar == opening then {\n"
        + "            set hangar to closing\n"
        + "            call sprocket.show_text(\"closing\") on [ screen ]\n"
        + "        }\n"
        + "        else {\n"
        + "            set hangar to opening\n"
        + "            call sprocket.show_text(\"opening\") on [ screen ]\n"
        + "        }\n"
        + "        set sprocket.power on [ light ] to on\n"
        + "    }\n"
        + "\n"
        + "    sequence 2 as 'opening gate' {\n"
        + "        call sprocket.depressurize(0) on [ 'hangar vent' ]\n"
        + "        wait 0.5s\n"
        + "        if any sprocket.flow_out_of_the_room on [ 'hangar vent' ] == 0 then {\n"
        + "            call sprocket.open(off) on [ gate ]\n"
        + "            await\n"
        + "        }\n"
        + "        await\n"
        + "        call sprocket.open(off) on [ gate ]\n"
        + "        await\n"
        + "        set hangar to open\n"
        + "        call sprocket.show_text(\"close\") on [ screen ]\n"
        + "        catch {\n"
        + "            call sprocket.open(off) on [ gate ]\n"
        + "            await\n"
        + "        }\n"
        + "    }\n"
        + "\n"
        + "    sequence 3 as 'closing gate' {\n"
        + "        call sprocket.close(off) on [ gate ]\n"
        + "        await\n"
        + "        call sprocket.pressurize(1) on [ 'hangar vent' ]\n"
        + "        await\n"
        + "        set hangar to close\n"
        + "        call sprocket.show_text(\"open\") on [ screen ]\n"
        + "    }\n"
        + "\n"
        + "    sequence 4 as done {\n"
        + "        set sprocket.power on [ light ] to off\n"
        + "    }\n"
        + "}\n\n"
        + "instance 1 of Reported.Hangar {\n"
        + "    'signal slot 1' = [ sprocket.toolbar(1, 1, 1) on [ panel1 ] ]\n"
        + "    'hangar vent' = [ vent1 ]\n"
        + "    gate = [ gate1 ]\n"
        + "    light = [ light1 ]\n"
        + "    screen = [ sprocket.get_screen(1) on [ screen1 ] ]\n"
        + "}\n";
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var runner = new AutomationRunner(program, rigged.World);
        var lines = new List<string>();
        runner.Log = new RunLog(lines.Add, () => "Hangar");
        runner.Tick(1);
        var system = program.Systems[0];
        runner.Call(system.FindSequence(1)!, system.Uses[0]);
        for (var tick = 2; tick <= 1200; tick++)
        {
            rigged.Tick();
            runner.Tick(tick);
        }

        Assert.Contains(lines, line => line.Contains("sequence 2 \"opening gate\"") && line.EndsWith("catch {"));
    }

    /// <summary>Every step of a sequence with handlers is named as its line of script for a log, the handlers by the lines that open them.</summary>
    [Fact]
    public void EveryStepIsNamedByItsLine()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Head + Handlers, program, Store));

        var named = program.Systems[0].FindSequence(1)!.Steps.Select(ProgramWriter.Line).ToList();

        Assert.Contains("catch {", named);
        Assert.Contains("finally {", named);
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
