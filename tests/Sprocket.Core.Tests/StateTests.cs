using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// What is switched off, and why, is kept in the block beside the program, not in it.
/// </summary>
/// <remarks>
/// It lists only what is off, so a construct with nothing parked keeps nothing at all, and a system somebody copies out of
/// a program carries no trace of what was switched off where it came from.
/// </remarks>
public class StateTests
{
    const string Airlock = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    static readonly ProtoBinary Binary = new();

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

    /// <summary>The switches of <paramref name="before" />, kept and read into a fresh ship.</summary>
    static Automation RoundTrip(Automation before)
    {
        var after = Ship();
        Assert.Equal("", BlockSwitches.Load(BlockSwitches.Save(before, Binary), after, Binary));
        return after;
    }

    /// <summary>A block with nothing switched off stores nothing.</summary>
    [Fact]
    public void NothingParkedWritesNothing()
    {
        Assert.Equal("", BlockSwitches.Save(Ship(), Binary));
    }

    /// <summary>What is off is kept and read back, with why where Sprocket switched it off.</summary>
    [Fact]
    public void WhatIsOffAndWhyIsKeptAndReadBack()
    {
        var before = Ship();
        before.Systems[0].Enabled = false;
        before.Systems[0].Halt = new Halt(SequenceProblem.MissingTarget, 2, 3, BlockTrouble.None, "Airlock 1 Outer Door");
        before.Systems[0].Instances[1].Enabled = false;
        before.Systems[0].Instances[1].Halt = new Halt(SequenceProblem.EmptySlot, 1, -1, BlockTrouble.None, "Inner Door");

        var system = RoundTrip(before).Systems[0];
        Assert.False(system.Enabled);
        Assert.Equal(SequenceProblem.MissingTarget, system.Halt!.Problem);
        Assert.Equal(2, system.Halt!.Sequence);
        Assert.Equal(3, system.Halt!.Step);
        Assert.Equal("Airlock 1 Outer Door", system.Halt!.Block);
        Assert.True(system.Instances[0].Enabled);
        Assert.False(system.Instances[1].Enabled);
        Assert.Equal(SequenceProblem.EmptySlot, system.Instances[1].Halt!.Problem);
        Assert.Equal(-1, system.Instances[1].Halt!.Step);
        Assert.Equal("Inner Door", system.Instances[1].Halt!.Block);
    }

    /// <summary>Something a person switched off reads back off, with no reason.</summary>
    [Fact]
    public void SwitchedOffByAPersonReadsBackWithNoReason()
    {
        var before = Ship();
        before.Systems[0].Instances[0].Enabled = false;

        var system = RoundTrip(before).Systems[0];
        Assert.True(system.Enabled);
        Assert.False(system.Instances[0].Enabled);
        Assert.Null(system.Instances[0].Halt);
    }

    /// <summary>Every problem that switches something off is kept and read back as itself, never as one not known.</summary>
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
            before.Systems[0].Halt = new Halt(problem, 2, 3, BlockTrouble.None, "Airlock 1 Outer Door");

            Assert.Equal(problem, RoundTrip(before).Systems[0].Halt!.Problem);
        }
    }

    /// <summary>What a block said stopped it is kept as what it is, and worded only when it is read.</summary>
    [Fact]
    public void ABlocksTroubleIsKeptAndReadBack()
    {
        var before = Ship();
        before.Systems[0].Enabled = false;
        before.Systems[0].Halt = new Halt(SequenceProblem.MissingTarget, 2, 3,
            BlockTrouble.Waiting(Cause.TankNoGas, "hydrogen"), "Airlock 1 Tank");

        var because = RoundTrip(before).Systems[0].Halt!.Because;
        Assert.Equal(Cause.TankNoGas, because.Cause);
        Assert.Equal("hydrogen", because.About);
        Assert.Equal("cannot fill because no hydrogen is coming in", TroubleText.Of(new Texts(), because));
    }

    /// <summary>A switch for a system or installation that is no longer there is dropped without a word.</summary>
    [Fact]
    public void ASwitchForSomethingGoneIsDropped()
    {
        var before = Ship();
        before.Systems[0].Instances[1].Enabled = false;
        var kept = BlockSwitches.Save(before, Binary);

        var after = Ship();
        after.Systems[0].RemoveInstance(after.Systems[0].Instances[1]);
        Assert.Equal("", BlockSwitches.Load(kept, after, Binary));
        Assert.True(after.Intact);
        Assert.All(after.Systems[0].Instances, instance => Assert.True(instance.Enabled));
    }

    /// <summary>Switches that would not unpack stop the program being saved over.</summary>
    /// <remarks>Saving runs whenever anything might have changed, so one edit after a half-read load would write the loss to disk.</remarks>
    [Fact]
    public void SwitchesThatWouldNotUnpackStopTheProgramBeingSaved()
    {
        var program = Ship();
        Assert.NotEqual("", BlockSwitches.Load("garbage!", program, Binary));

        Assert.False(program.Intact);
    }

    /// <summary>A block written in the earlier form is read as it was written, and kept in the packed form from then on.</summary>
    [Fact]
    public void TheEarlierFormIsReadAndKeptPacked()
    {
        var earlier =
            "off \"" + Airlock + "\" \"block gone\" sequence 2 step 3 block \"Airlock 1 Tank\" \"tank no gas\" \"hydrogen\"\n" +
            "off \"" + Airlock + "\" instance 2 \"slot empty\" sequence 1 block \"Inner Door\"\n";

        var read = Ship();
        Assert.Equal("", BlockSwitches.Load(earlier, read, Binary));

        var kept = BlockSwitches.Save(read, Binary);
        Assert.DoesNotContain(" ", kept);

        var system = RoundTrip(read).Systems[0];
        Assert.False(system.Enabled);
        Assert.Equal(SequenceProblem.MissingTarget, system.Halt!.Problem);
        Assert.Equal(2, system.Halt!.Sequence);
        Assert.Equal(3, system.Halt!.Step);
        Assert.Equal("Airlock 1 Tank", system.Halt!.Block);
        Assert.Equal(Cause.TankNoGas, system.Halt!.Because.Cause);
        Assert.Equal("hydrogen", system.Halt!.Because.About);
        Assert.False(system.Instances[1].Enabled);
        Assert.Equal(SequenceProblem.EmptySlot, system.Instances[1].Halt!.Problem);
        Assert.Equal("Inner Door", system.Instances[1].Halt!.Block);
    }

    /// <summary>Owners and a pause written by the earlier form are not read into the block.</summary>
    [Fact]
    public void OwnersInTheEarlierFormAreTrouble()
    {
        var after = Ship();
        Assert.NotEqual("", BlockSwitches.Load("owned 144115188075855874\n", after, Binary));
        Assert.Empty(after.Owners);
    }

    /// <summary>A system switched off by its identity alone, in the earlier form, is its own switch, and leaves each installation's as it was.</summary>
    [Fact]
    public void AWholeSystemSwitchedOffIsItsOwnSwitch()
    {
        var after = Ship();
        Assert.Equal("", BlockSwitches.Load("off \"" + Airlock + "\"\n", after, Binary));

        Assert.False(after.Systems[0].Enabled);
        Assert.All(after.Systems[0].Instances, instance => Assert.True(instance.Enabled));
        Assert.Null(after.Systems[0].Halt);
    }

    /// <summary>
    /// A line of the earlier form about something that is no longer there is dropped without a word, and one naming a
    /// sequence switches nothing, since a sequence has no switch of its own.
    /// </summary>
    /// <remarks>
    /// Deleting a system is the ordinary case this has to survive. Reporting it would mark the program damaged for having
    /// been edited, which would stop it being saved at all.
    /// </remarks>
    [Theory]
    [InlineData("off \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\"")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" sequence 9")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" instance 9")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" sequence 2")]
    public void ALineThatNamesNothingToSwitchSwitchesNothing(string line)
    {
        var after = Ship();
        Assert.Equal("", BlockSwitches.Load(line + "\n", after, Binary));

        Assert.True(after.Systems[0].Enabled);
        Assert.All(after.Systems[0].Instances, instance => Assert.True(instance.Enabled));
    }

    /// <summary>A reason of the earlier form this version cannot read is kept as one not known, and said shortly.</summary>
    [Fact]
    public void AReasonNotKnownIsSaidShortly()
    {
        var after = Ship();
        Assert.Equal("", BlockSwitches.Load("off \"" + Airlock + "\" \"something new\"\n", after, Binary));

        var system = after.Systems[0];
        Assert.False(system.Enabled);
        Assert.Equal(SequenceProblem.None, system.Halt!.Problem);
        Assert.Equal("Stopped for an unknown reason.", SequenceText.Stopped(new Texts(), system.Halt, system, false));
    }

    /// <summary>A line of the earlier form that says something other than what is off is trouble.</summary>
    [Theory]
    [InlineData("off MalTek.Airlock")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" sequence")]
    [InlineData("off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" whatever 2")]
    [InlineData("on \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\"")]
    public void ALineThatSaysSomethingElseIsTrouble(string line)
    {
        var program = Ship();
        Assert.NotEqual("", BlockSwitches.Load(line + "\n", program, Binary));
        Assert.False(program.Intact);
    }

    /// <summary>
    /// A number of the earlier form past what a number can hold is trouble rather than the sequence it wraps onto.
    /// </summary>
    [Theory]
    [InlineData("sequence 4294967297")]
    [InlineData("sequence 0")]
    [InlineData("instance -1")]
    public void ANumberThatIsNotASequenceOrInstanceNumberIsTrouble(string tail)
    {
        var program = Ship();
        Assert.NotEqual("", BlockSwitches.Load("off \"" + Airlock + "\" " + tail + "\n", program, Binary));

        Assert.True(program.Systems[0].Enabled);
        Assert.True(program.Systems[0].Instances[0].Enabled);
    }

    /// <summary>A program written from a construct with its systems switched off reads back with everything running.</summary>
    [Fact]
    public void AProgramCarriesNoSwitches()
    {
        var program = Ship();
        program.Systems[0].Enabled = false;
        program.Systems[0].Halt = new Halt(SequenceProblem.NeverTriggered, 1, -1, BlockTrouble.None, null);
        program.Systems[0].Instances[0].Enabled = false;

        var text = ProgramWriter.Save(program, new Values());

        var after = new Automation();
        Assert.Equal("", ProgramReader.Load(text, after, new Values()));
        Assert.True(after.Systems[0].Enabled);
        Assert.Null(after.Systems[0].Halt);
        Assert.True(after.Systems[0].Instances[0].Enabled);
    }
}
