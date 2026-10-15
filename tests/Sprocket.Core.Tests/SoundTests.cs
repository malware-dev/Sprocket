using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A sound block playing the sound a step chooses, from the block's own list.
/// </summary>
/// <remarks>
/// The rig loads no definitions, so it names each sound by its id. The names the terminal shows are
/// read from the game's sound categories, and are proved in the game alone.
/// </remarks>
public class SoundTests
{
    [Fact]
    public void PlayingAsksForASoundFromTheBlocksOwnList()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        var kind = ship.Entity(speaker.Id).Kind;

        var play = kind.FindAction(Blocks.Speaker.Play, Nature.Native)!;
        Assert.False(play.Lasts);

        var fields = new List<Field>();
        play.Fields(fields);
        Assert.Single(fields);
        Assert.Equal(Editor.Chosen, fields[0].Edits);
        Assert.True(fields[0].Needed);
        Assert.Equal(new[] { "SoundBlockAlert1", "SoundBlockAlert2", "SoundBlockLightsOn" }, fields[0].Means);

        // The game's own play plays whichever sound was left selected.
        Assert.True(kind.FindAction("PlaySound", Nature.Action)!.Hidden);
        Assert.False(kind.FindAction("StopSound", Nature.Action)!.Hidden);
    }

    [Fact]
    public void AStepPlaysTheSoundItChose()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        speaker.Selected = "SoundBlockAlert1";

        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var play = new Act(Blocks.Speaker.Play);
        play.Targets.Add(automation.Declare(ship.World, ship.Entity(speaker.Id)).Name);
        play.Arguments.Add(Literal.Of("SoundBlockAlert2"));
        sequence.Steps.Add(play);

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);
        ship.Tick();
        runner.Tick(2);

        Assert.Contains("Speaker:play SoundBlockAlert2", ship.Rig.Log);
    }

    /// <summary>The sound the block holds ready is one of its sounds, by id, and none of them where none is chosen.</summary>
    [Fact]
    public void TheSelectedSoundIsOneOfItsSounds()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        var selected = ship.Entity(speaker.Id).Kind.FindAction(Blocks.Speaker.Selected, Nature.Value)!;

        Assert.Equal(new[] { "SoundBlockAlert1", "SoundBlockAlert2", "SoundBlockLightsOn" }, selected.Being!.Names);
        Assert.False(selected.Being.Is(Literal.Name("SoundBlockAlert2")).Holds(speaker.Block, null!));

        speaker.Selected = "SoundBlockAlert2";
        Assert.True(selected.Being.Is(Literal.Name("SoundBlockAlert2")).Holds(speaker.Block, null!));
        Assert.Equal(Literal.Of(1L), Ran.Read(ship, speaker, Blocks.Speaker.Selected));
    }

    /// <summary>The block's own stop, volume, range and loop time are said in Sprocket's words.</summary>
    [Fact]
    public void ItsOwnMembersAreSaidInSprocketsWords()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        speaker.Carrying("VolumeSlider", "Volume", ValueKind.Real)
            .Carrying("RangeSlider", "Range", ValueKind.Real)
            .Carrying("LoopableSlider", "Loop time", ValueKind.Real);
        var kind = ship.Entity(speaker.Id).Kind;

        Assert.Equal("stop", kind.FindAction("StopSound", Nature.Action)!.Text);
        Assert.Equal("volume", kind.FindAction("VolumeSlider", Nature.Value)!.Text);
        Assert.Equal("range", kind.FindAction("RangeSlider", Nature.Value)!.Text);
        Assert.Equal("loop time", kind.FindAction("LoopableSlider", Nature.Value)!.Text);
    }

    /// <summary>
    /// A jukebox is a sound block to the game and not to Sprocket: no sound outside its playlist, nothing to stop, no loop,
    /// and its volume and range in Sprocket's words.
    /// </summary>
    [Fact]
    public void AJukeboxPlaysNoSingleSound()
    {
        var ship = new Rigged();
        var jukebox = ship.Rig.Jukebox(1, "Jukebox");
        jukebox.Carrying("VolumeSlider", "Volume", ValueKind.Real)
            .Carrying("LoopableSlider", "Loop time", ValueKind.Real);
        var kind = ship.Entity(jukebox.Id).Kind;

        Assert.Null(kind.FindAction(Blocks.Speaker.Play, Nature.Native));
        Assert.Null(kind.FindAction(Blocks.Speaker.Selected, Nature.Value));
        Assert.True(kind.FindAction("PlaySound", Nature.Action)!.Hidden);
        Assert.True(kind.FindAction("StopSound", Nature.Action)!.Hidden);
        Assert.True(kind.FindAction("LoopableSlider", Nature.Value)!.Hidden);
        Assert.Equal("volume", kind.FindAction("VolumeSlider", Nature.Value)!.Text);
    }

    /// <summary>
    /// A sound block vouches for its play, its selected sound, its stop and its sliders, and not for the game's own play or
    /// the steps of a slider.
    /// </summary>
    [Fact]
    public void ASpeakerVouchesForItsMembersAndNotTheirSteps()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        speaker.Carrying("VolumeSlider", "Volume", ValueKind.Real)
            .Carrying("RangeSlider", "Range", ValueKind.Real)
            .Carrying("LoopableSlider", "Loop time", ValueKind.Real)
            .Offering("IncreaseVolumeSlider", "Increase Volume")
            .Offering("DecreaseRangeSlider", "Decrease Range")
            .Offering("IncreaseLoopableSlider", "Increase Loop time");
        var kind = ship.Entity(speaker.Id).Kind;

        Assert.True(kind.FindAction(Blocks.Speaker.Play, Nature.Native)!.Vetted);
        Assert.True(kind.FindAction(Blocks.Speaker.Selected, Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("StopSound", Nature.Action)!.Vetted);
        Assert.True(kind.FindAction("VolumeSlider", Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("RangeSlider", Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("LoopableSlider", Nature.Value)!.Vetted);

        Assert.False(kind.FindAction("PlaySound", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("IncreaseVolumeSlider", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("DecreaseRangeSlider", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("IncreaseLoopableSlider", Nature.Action)!.Vetted);
    }

    /// <summary>A jukebox vouches for its volume and range, and for nothing its own terminal hides or steps.</summary>
    [Fact]
    public void AJukeboxVouchesForItsVolumeAndRangeAlone()
    {
        var ship = new Rigged();
        var jukebox = ship.Rig.Jukebox(1, "Jukebox");
        jukebox.Carrying("VolumeSlider", "Volume", ValueKind.Real)
            .Carrying("RangeSlider", "Range", ValueKind.Real)
            .Carrying("LoopableSlider", "Loop time", ValueKind.Real)
            .Offering("IncreaseVolumeSlider", "Increase Volume");
        var kind = ship.Entity(jukebox.Id).Kind;

        Assert.True(kind.FindAction("VolumeSlider", Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("RangeSlider", Nature.Value)!.Vetted);

        Assert.False(kind.FindAction("PlaySound", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("StopSound", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("LoopableSlider", Nature.Value)!.Vetted);
        Assert.False(kind.FindAction("IncreaseVolumeSlider", Nature.Action)!.Vetted);
    }

    /// <summary>A step sets the sound the block holds ready, by its id, without playing it.</summary>
    [Fact]
    public void AStepChoosesTheSelectedSound()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        speaker.Selected = "SoundBlockAlert1";

        Assert.Equal(SequenceProblem.None, Ran.Set(ship, speaker, Blocks.Speaker.Selected, Literal.Name("SoundBlockLightsOn")));

        Assert.Equal("SoundBlockLightsOn", speaker.Selected);
        Assert.DoesNotContain(ship.Rig.Log, line => line.StartsWith("Speaker:play"));
    }

    /// <summary>A sound the block does not have is a fault, and leaves the sound it holds as it was.</summary>
    [Fact]
    public void ASoundTheBlockLacksIsAFault()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        speaker.Selected = "SoundBlockAlert1";

        Assert.Equal(SequenceProblem.Mistyped, Ran.Set(ship, speaker, Blocks.Speaker.Selected, Literal.Name("SoundBlockAlert9")));

        Assert.Equal("SoundBlockAlert1", speaker.Selected);
    }

    [Fact]
    public void TheStepReadsWithTheSoundsName()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        var play = ship.Entity(speaker.Id).Kind.FindAction(Blocks.Speaker.Play, Nature.Native)!;
        var fields = new List<Field>();
        play.Fields(fields);

        // The rig's names are its ids; the game's are the terminal's, by the same field.
        fields[0] = Field.Among("sound", ["Alert 1", "Alert 2"], ["SoundBlockAlert1", "SoundBlockAlert2"]);

        Assert.Equal("Alert 2", SentenceText.Given(new ArgumentsSlot([Literal.Of("SoundBlockAlert2")], fields, null)));
    }
}
