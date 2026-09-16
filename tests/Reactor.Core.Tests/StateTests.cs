using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.World;
using Reactor.Host.SpaceEngineers.Storage;
using Xunit;

namespace Reactor.Tests;

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
        system.AddInstance();

        program.AddSequence(system);
        program.AddSequence(system);

        return program;
    }

    [Fact]
    public void NothingParkedWritesNothing()
    {
        Assert.Equal("", StateText.Save(Ship()));
    }

    [Fact]
    public void WhatIsOffIsWrittenAndReadBack()
    {
        var before = Ship();
        before.Systems[0].Sequences[1].Enabled = false;
        before.Systems[0].Instances[1].Enabled = false;

        var text = StateText.Save(before);
        Assert.Equal(
            "off \"" + Airlock + "\" sequence 2\n" +
            "off \"" + Airlock + "\" instance 2\n",
            text);

        var after = Ship();
        Assert.Equal("", StateText.Load(text, after));

        Assert.True(after.Systems[0].Enabled);
        Assert.True(after.Systems[0].Sequences[0].Enabled);
        Assert.False(after.Systems[0].Sequences[1].Enabled);
        Assert.True(after.Systems[0].Instances[0].Enabled);
        Assert.False(after.Systems[0].Instances[1].Enabled);
    }

    [Fact]
    public void AWholeSystemIsSwitchedOffByItsIdentityAlone()
    {
        var before = Ship();
        before.Systems[0].Enabled = false;

        Assert.Equal("off \"" + Airlock + "\"\n", StateText.Save(before));

        var after = Ship();
        Assert.Equal("", StateText.Load(StateText.Save(before), after));
        Assert.False(after.Systems[0].Enabled);
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
        Assert.Equal("", StateText.Load(line + "\n", after));

        Assert.True(after.Systems[0].Enabled);
        Assert.True(after.Systems[0].Sequences[1].Enabled);
        Assert.True(after.Systems[0].Instances[1].Enabled);
    }

    [Theory]
    [InlineData("off MalTek.Airlock")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" sequence")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" whatever 2")]
    [InlineData("on \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\"")]
    public void ALineThatSaysSomethingElseIsTrouble(string line)
    {
        Assert.NotEqual("", StateText.Load(line + "\n", Ship()));
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
        Assert.NotEqual("", StateText.Load("off MalTek.Airlock\n", program));

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
        Assert.NotEqual("", StateText.Load("off \"" + Airlock + "\" " + tail + "\n", program));

        Assert.True(program.Systems[0].Sequences[0].Enabled);
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
        program.Systems[0].Sequences[0].Enabled = false;
        program.Systems[0].Instances[0].Enabled = false;

        var text = ProgramText.Save(program, new Values());

        Assert.DoesNotContain(" is on", text);
        Assert.DoesNotContain(" is off", text);

        // And a program read without its switches is a construct with everything running.
        var after = new Automation();
        Assert.Equal("", ProgramText.Load(text, after, new Values()));
        Assert.True(after.Systems[0].Enabled);
        Assert.True(after.Systems[0].Sequences[0].Enabled);
        Assert.True(after.Systems[0].Instances[0].Enabled);
    }
}
