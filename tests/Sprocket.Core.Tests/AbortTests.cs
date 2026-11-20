using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Xunit;

namespace Sprocket.Tests;

/// <summary>An abort step ends the sequence it is in, at once, saying why at its level; as a problem it also switches the installation off.</summary>
public class AbortTests
{
    static readonly string Version = $"{ScriptFormat.Line}\n\n";

    const string Header = "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n";

    static readonly Values Store = new Values();

    /// <summary>Each form of abort reads back as it was written: bare, with a message, and with a message and a level.</summary>
    [Fact]
    public void AnAbortIsWrittenAsItWasRead()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Version + Header
            + "    sequence 1 {\n"
            + "        abort\n"
            + "        abort saying \"Door jammed\"\n"
            + "        abort saying \"Hangar \"\"B\"\" blocked\" as problem\n"
            + "        abort as warning\n"
            + "    }\n"
            + "}\n", program, Store));

        var written = ProgramWriter.Save(program, Store);

        Assert.Contains("        abort\n", written);
        Assert.Contains("        abort saying \"Door jammed\"\n", written);
        Assert.Contains("        abort saying \"Hangar \"\"B\"\" blocked\" as problem\n", written);
        Assert.Contains("        abort as warning\n", written);
    }

    /// <summary>A program naming a state's choices note, warning and problem reads, the words being an abort's only within an abort step.</summary>
    [Fact]
    public void TheLevelWordsStayFreeAsNames()
    {
        var program = new Automation();

        Assert.Equal("", ProgramReader.Load(Version + Header
            + "    type level = note | warning | problem\n\n"
            + "    let saying = warning\n\n"
            + "    sequence 1 {\n        set saying to problem\n    }\n"
            + "}\n", program, Store));
    }

    /// <summary>An abort ends the run where it stands, the steps after it left undone, and says its message at its level; only a problem switches the logic system off.</summary>
    [Theory]
    [InlineData("note", SequenceProblem.Aborted)]
    [InlineData("warning", SequenceProblem.AbortedWarning)]
    [InlineData("problem", SequenceProblem.AbortedProblem)]
    public void AnAbortEndsTheRunAndSaysWhy(string level, SequenceProblem problem)
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Version + Header
            + "    let hits = off\n\n"
            + "    sequence 1 {\n"
            + "        set hits to on\n"
            + $"        abort saying \"Door jammed\" as {level}\n"
            + "        set hits to off\n"
            + "    }\n"
            + "}\n", program, Store));
        var runner = new AutomationRunner(program, new Rigged().World);
        runner.Tick(1);
        var system = program.Systems[0];
        var self = system.Uses[0];
        var sequence = system.FindSequence(1)!;

        runner.Call(sequence, self, true);
        runner.Tick(2);
        runner.Tick(3);

        var run = runner.Get(sequence, self)!;
        Assert.False(run.IsRunning);
        Assert.Equal(Literal.Of(true), runner.States.Read(system, self, system.FindState("hits")!));
        var fault = SequenceSummary.Fault(run);
        Assert.Equal(problem, fault.Problem);
        Assert.Equal("Door jammed", fault.Which);
        Assert.Equal(problem != SequenceProblem.AbortedProblem, system.Enabled);
        if (problem == SequenceProblem.AbortedProblem)
            Assert.Equal("Door jammed", system.Halt!.Block);
    }

    /// <summary>A bare abort ends the run where it stands and says nothing: the sequence reads as one that finished.</summary>
    [Fact]
    public void ABareAbortEndsTheRunSilently()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Version + Header
            + "    let hits = off\n\n"
            + "    sequence 1 {\n"
            + "        abort\n"
            + "        set hits to on\n"
            + "    }\n"
            + "}\n", program, Store));
        var runner = new AutomationRunner(program, new Rigged().World);
        runner.Tick(1);
        var system = program.Systems[0];
        var self = system.Uses[0];
        var sequence = system.FindSequence(1)!;

        runner.Call(sequence, self, true);
        runner.Tick(2);
        runner.Tick(3);

        var run = runner.Get(sequence, self)!;
        Assert.False(run.IsRunning);
        Assert.Equal(Literal.Of(false), runner.States.Read(system, self, system.FindState("hits")!));
        Assert.DoesNotContain(SequenceSummary.Fault(run).Problem, new[] { SequenceProblem.Aborted, SequenceProblem.AbortedWarning, SequenceProblem.AbortedProblem });
        Assert.Equal(SequenceStatus.Waiting, SequenceSummary.Status(sequence, run));
        Assert.True(system.Enabled);
    }

    /// <summary>A copy of a sequence keeps its abort, message and level, and aborts itself: an abort names no sequence.</summary>
    [Fact]
    public void ACopyKeepsItsAbort()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Version + Header
            + "    sequence 1 {\n        abort saying \"Door jammed\" as warning\n    }\n"
            + "}\n", program, Store));

        var copy = program.Duplicate(program.Systems[0].FindSequence(1)!);

        var abort = Assert.IsType<Abort>(Assert.Single(copy.Steps));
        Assert.Equal("Door jammed", abort.Message);
        Assert.Equal(AbortLevel.Warning, abort.Level);
    }
}
