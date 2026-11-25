using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Sim.Fakes;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Every trigger and step of the language allocates nothing on its repeating path: a trigger as it watches, and a step as
/// a sequence that holds it runs again and again. Counted apart from what the simulator's stand-ins for the game's blocks
/// allocate, which the game's own blocks do not.
/// </summary>
public class LanguageCostTests
{
    const int Doors = 10;
    // Long enough for every rota list a row uses to have taken a shift once, which makes its array once: a cost of settling
    // in, never of running again.
    const int Warm = 600;
    const int Ticks = 1000;

    static readonly Values Store = new Values();

    readonly ITestOutputHelper _said;

    /// <summary>The tests, saying what they measured to <paramref name="said" />.</summary>
    public LanguageCostTests(ITestOutputHelper said)
    {
        _said = said;
    }

    static string Named()
    {
        var names = new List<string>();
        for (var i = 1; i <= Doors; i++)
            names.Add("door" + i);

        return "[ " + string.Join(", ", names) + " ]";
    }

    // The program: ten shut doors, the system's states, the line given, two whens on the states a set step writes, a first sequence of the steps given, and a second
    // sequence for run and stop to name.
    static string Program(string line, string steps)
    {
        var text = "sprocket 3 alpha 3\n\n";
        for (var i = 1; i <= Doors; i++)
            text += "let door" + i + " = block 'Door/LargeBlockDoor' [ " + (1000 + i) + " ]\n";

        return text + "\nsystem \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Works\n{\n"
            + "    let cycling = off\n"
            + "    let count = 0\n\n"
            + "    " + line + "\n\n"
            + "    sequence 1 {\n" + steps + "    }\n\n"
            + "    sequence 2 {\n        wait 600t\n    }\n}\n";
    }

    // The bytes the runner allocates of its own over the measured ticks, after it has settled, and how many questions it
    // asked the blocks.
    static long Measured(string line, string steps, out int asked, out Automation program, out AutomationRunner runner)
    {
        var ship = new Rigged();
        for (var i = 1; i <= Doors; i++)
            ship.Door(1000 + i, "Door " + i);

        program = new Automation();
        Assert.Equal("", ProgramReader.Load(Program(line, steps), program, Store));
        runner = new AutomationRunner(program, ship.World);
        var frame = 0;
        for (; frame < Warm; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        long own = 0;
        asked = Pretend.Asked;
        var running = runner;
        for (var end = frame + Ticks; frame < end; frame++)
        {
            ship.Tick();
            var tick = frame + 1;
            own += StandInLedger.Own(() => running.Tick(tick));
        }

        asked = Pretend.Asked - asked;
        return own;
    }

    /// <summary>A trigger watching blocks or a state that do not change allocates nothing as it looks.</summary>
    [Theory]
    [InlineData("when any sprocket.status on DOORS == open then run 1")]
    [InlineData("when all sprocket.status on DOORS == open then run 1")]
    [InlineData("when sum sprocket.how_far_open on DOORS > 50 then run 1")]
    [InlineData("when any sprocket.how_far_open on DOORS > 50 every 30t then run 1")]
    [InlineData("whenever sprocket.finishes_opening on DOORS then run 1")]
    [InlineData("when cycling == on then run 1")]
    [InlineData("when any sprocket.status on DOORS == open or cycling then run 1")]
    [InlineData("when count + 1 > 3 then run 1")]
    public void AWatchingTriggerAllocatesNothing(string line)
    {
        line = line.Replace("DOORS", Named());
        var own = Measured(line, "        wait 1t\n", out var asked, out _, out _);

        _said.WriteLine($"{line}: {own} bytes of its own over {Ticks} ticks, {asked} questions to the blocks");
        Assert.Equal(0, own);
    }

    /// <summary>
    /// A step in a sequence an every runs again and again allocates nothing as it runs, and nothing as it waits; a run that
    /// outlasts the every's turn is skipped once, as a busy sequence is.
    /// </summary>
    [Theory]
    [InlineData("wait 10t")]
    [InlineData("wait count * 1t + 5t")]
    [InlineData("call sprocket.open on [ door1 ]")]
    [InlineData("call sprocket.open on [ door1 ]\n        await\n        call sprocket.close on [ door1 ]\n        await")]
    [InlineData("set cycling to on")]
    [InlineData("set cycling to count > 3")]
    [InlineData("if any sprocket.status on [ door1 ] == open then {\n            wait 1t\n        }")]
    [InlineData("if cycling or count > 1 then {\n            wait 1t\n        }\n        else {\n            wait 2t\n        }")]
    [InlineData("run 2")]
    [InlineData("run 2\n        stop 2")]
    [InlineData("wait 1t\n        abort")]
    [InlineData("wait 1t\n        catch {\n            set cycling to on\n        }\n        finally {\n            set cycling to off\n        }")]
    public void ARepeatedStepAllocatesNothing(string steps)
    {
        var own = Measured("every 60t run 1", "        " + steps + "\n", out _, out var program, out var runner);

        var system = program.Systems[0];
        var fired = runner.Get(system.FindSequence(1)!, system.Uses[0])!.FiredCount;
        _said.WriteLine($"{steps.Replace("\n", " ")}: {own} bytes of its own over {Ticks} ticks, run {fired} times");
        Assert.True(fired >= (Warm + Ticks) / 120, $"the sequence ran {fired} times");
        Assert.Equal(0, own);
    }
}
