using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Storage;
using Xunit;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Tests;

/// <summary>
/// What is switched off is written beside the program, not in it.
/// </summary>
/// <remarks>
/// Two documents rather than one, and this is the half that changes at the press of a button. It
/// lists only what is off, so a construct with nothing parked writes nothing at all - and a system
/// somebody copies out of a program carries no trace of what was switched off where it came from.
/// </remarks>
public class StateTests
{
    const string Airlock = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    static Automation Ship()
    {
        var program = new Automation();

        var system = program.AddSystem();
        system.Identity = new Guid(Airlock);
        system.Namespace = "MalTek";
        system.Name = "Airlock";
        system.IsBlueprint = true;
        system.AddInstance();
        system.AddInstance();

        program.AddSequence(system);

        program.AddSequence(system);

        return program;
    }

    [Fact]
    public void NothingParkedWritesNothing()
    {
        Assert.Equal("", StateText.Save(Ship(), null));
    }

    /// <summary>What is off is written and read back, with why where Sprocket switched it off.</summary>
    [Fact]
    public void WhatIsOffAndWhyIsWrittenAndReadBack()
    {
        var before = Ship();
        before.Systems[0].Enabled = false;
        before.Systems[0].Halt = new Halt(SequenceProblem.MissingTarget, 2, 3, Host.SpaceEngineers.World.Trouble.None, "Airlock 1 Outer Door");
        before.Systems[0].Instances[1].Enabled = false;
        before.Systems[0].Instances[1].Halt = new Halt(SequenceProblem.EmptySlot, 1, -1, Host.SpaceEngineers.World.Trouble.None, "Inner Door");

        var text = StateText.Save(before, null);
        Assert.Equal(
            "off \"" + Airlock + "\" \"block gone\" sequence 2 step 3 block \"Airlock 1 Outer Door\"\n" +
            "off \"" + Airlock + "\" instance 2 \"slot empty\" sequence 1 block \"Inner Door\"\n",
            text);

        var after = Ship();
        Assert.Equal("", StateText.Load(text, after, null));

        var system = after.Systems[0];
        Assert.False(system.Enabled);
        Assert.Equal(SequenceProblem.MissingTarget, system.Halt!.Problem);
        Assert.Equal(2, system.Halt!.Sequence);
        Assert.Equal(3, system.Halt!.Step);
        Assert.Equal("Airlock 1 Outer Door", system.Halt!.Block);
        Assert.True(system.Instances[0].Enabled);
        Assert.False(system.Instances[1].Enabled);
        Assert.Equal(SequenceProblem.EmptySlot, system.Instances[1].Halt!.Problem);
        Assert.Equal("Inner Door", system.Instances[1].Halt!.Block);
    }

    /// <summary>Every problem that switches something off is written and read back as itself, never as one not known.</summary>
    /// <remarks>Walks every problem there is, so one added later that switches things off and has no spelling fails here.</remarks>
    [Fact]
    public void EveryReasonForSwitchingOffSurvivesAReload()
    {
        foreach (SequenceProblem problem in Enum.GetValues(typeof(SequenceProblem)))
        {
            if (!new SequenceFault(problem, 0).Parks)
                continue;

            var before = Ship();
            before.Systems[0].Enabled = false;
            before.Systems[0].Halt = new Halt(problem, 2, 3, Host.SpaceEngineers.World.Trouble.None, "Airlock 1 Outer Door");

            var after = Ship();
            Assert.Equal("", StateText.Load(StateText.Save(before, null), after, null));
            Assert.Equal(problem, after.Systems[0].Halt!.Problem);
        }
    }

    /// <summary>What a block said stopped it is kept as what it is, and worded only when it is read.</summary>
    [Fact]
    public void ABlocksTroubleIsWrittenAndReadBack()
    {
        var before = Ship();
        before.Systems[0].Enabled = false;
        before.Systems[0].Halt = new Halt(SequenceProblem.MissingTarget, 2, 3,
            Host.SpaceEngineers.World.Trouble.Waiting(Cause.TankNoGas, "hydrogen"), "Airlock 1 Tank");

        var text = StateText.Save(before, null);
        Assert.Equal(
            "off \"" + Airlock + "\" \"block gone\" sequence 2 step 3 block \"Airlock 1 Tank\" \"tank no gas\" \"hydrogen\"\n",
            text);

        var after = Ship();
        Assert.Equal("", StateText.Load(text, after, null));

        var because = after.Systems[0].Halt!.Because;
        Assert.Equal(Cause.TankNoGas, because.Cause);
        Assert.Equal("hydrogen", because.About);
        Assert.Equal("cannot fill - no hydrogen coming in", TroubleText.Of(new Texts(), because));
    }

    /// <summary>A system switched off by its identity alone is its own switch, and leaves each one's switch and each sequence as they were.</summary>
    [Fact]
    public void AWholeSystemSwitchedOffIsItsOwnSwitch()
    {
        var after = Ship();
        Assert.Equal("", StateText.Load("off \"" + Airlock + "\"\n", after, null));

        Assert.False(after.Systems[0].Enabled);
        Assert.All(after.Systems[0].Instances, instance => Assert.True(instance.Enabled));
        Assert.Null(after.Systems[0].Halt);
    }

    /// <summary>
    /// A line about something that is no longer there is dropped without a word.
    /// </summary>
    /// <remarks>
    /// Deleting a system is the ordinary case this has to survive. Reporting it would mark the
    /// program damaged for having been edited, which would stop it being saved at all.
    /// </remarks>
    [Theory]
    [InlineData("off \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\"")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" sequence 9")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" instance 9")]
    public void ALineAboutSomethingGoneIsDropped(string line)
    {
        var after = Ship();
        Assert.Equal("", StateText.Load(line + "\n", after, null));

        Assert.True(after.Systems[0].Enabled);
        Assert.True(after.Systems[0].Instances[1].Enabled);
    }

    /// <summary>A line naming a sequence switches nothing, since a sequence has no switch of its own.</summary>
    [Fact]
    public void ALineForASequenceSwitchesNothing()
    {
        var after = Ship();
        Assert.Equal("", StateText.Load("off \"" + Airlock + "\" sequence 2\n", after, null));

        Assert.True(after.Systems[0].Enabled);
        Assert.All(after.Systems[0].Instances, instance => Assert.True(instance.Enabled));
    }

    /// <summary>A reason this version cannot read is kept as one not known, and said shortly.</summary>
    [Fact]
    public void AReasonNotKnownIsSaidShortly()
    {
        var after = Ship();
        Assert.Equal("", StateText.Load("off \"" + Airlock + "\" \"something new\"\n", after, null));

        var system = after.Systems[0];
        Assert.False(system.Enabled);
        Assert.Equal(SequenceProblem.None, system.Halt!.Problem);
        Assert.Equal("Stopped for an unknown reason.", SequenceText.Stopped(new Texts(), system.Halt, system));
    }

    [Theory]
    [InlineData("off MalTek.Airlock")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" sequence")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" whatever 2")]
    [InlineData("on \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\"")]
    public void ALineThatSaysSomethingElseIsTrouble(string line)
    {
        Assert.NotEqual("", StateText.Load(line + "\n", Ship(), null));
    }

    /// <summary>
    /// A switches document that would not read stops the program being saved over.
    /// </summary>
    /// <remarks>
    /// The same promise the program itself makes, and it moved with the switches: saving runs whenever
    /// anything might have changed, so one edit after a half-read load would write the loss to disk.
    /// </remarks>
    [Fact]
    public void SwitchesThatWouldNotReadStopTheProgramBeingSaved()
    {
        var program = Ship();
        Assert.NotEqual("", StateText.Load("off MalTek.Airlock\n", program, null));

        Assert.False(program.Intact);
    }

    /// <summary>
    /// A number past what a number can hold is trouble rather than the sequence it wraps onto.
    /// </summary>
    [Theory]
    [InlineData("sequence 4294967297")]
    [InlineData("sequence 0")]
    [InlineData("instance -1")]
    public void ANumberThatIsNotASequenceOrInstanceNumberIsTrouble(string tail)
    {
        var program = Ship();
        Assert.NotEqual("", StateText.Load("off \"" + Airlock + "\" " + tail + "\n", program, null));

        Assert.True(program.Systems[0].Enabled);
        Assert.True(program.Systems[0].Instances[0].Enabled);
    }

    /// <summary>
    /// The program itself says nothing about switches, so copying a system carries none.
    /// </summary>
    [Fact]
    public void TheProgramSaysNothingAboutSwitches()
    {
        var program = Ship();
        program.Systems[0].Enabled = false;
        program.Systems[0].Halt = new Halt(SequenceProblem.NeverTriggered, 1, -1, Host.SpaceEngineers.World.Trouble.None, null);
        program.Systems[0].Instances[0].Enabled = false;

        var text = ProgramText.Save(program, new Values());

        Assert.DoesNotContain(" is on", text);
        Assert.DoesNotContain(" is off", text);

        // And a program read without its switches is a construct with everything running.
        var after = new Automation();
        Assert.Equal("", ProgramText.Load(text, after, new Values()));
        Assert.True(after.Systems[0].Enabled);
        Assert.Null(after.Systems[0].Halt);
        Assert.True(after.Systems[0].Instances[0].Enabled);
    }
}
