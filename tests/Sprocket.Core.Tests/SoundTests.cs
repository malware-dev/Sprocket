using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Words;
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

        var play = kind.FindAction(SoundExtension.Play, Form.Native)!;
        Assert.False(play.Lasts);

        var fields = new List<Field>();
        play.Fields(fields);
        Assert.Single(fields);
        Assert.Equal(Editor.Chosen, fields[0].Edits);
        Assert.True(fields[0].Needed);
        Assert.Equal(new[] { "SoundBlockAlert1", "SoundBlockAlert2", "SoundBlockLightsOn" }, fields[0].Means);

        // The game's own play plays whichever sound was left selected.
        Assert.True(kind.FindAction("PlaySound", Form.Action)!.Hidden);
        Assert.False(kind.FindAction("StopSound", Form.Action)!.Hidden);
    }

    [Fact]
    public void AStepPlaysTheSoundItChose()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        speaker.Selected = "SoundBlockAlert1";

        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var play = new Act(SoundExtension.Play);
        play.Targets.Add(automation.Declare(ship.World, ship.Entity(speaker.Id)).Name);
        play.Values.Add(Literal.Of("SoundBlockAlert2"));
        sequence.Steps.Add(play);

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);
        ship.Tick();
        runner.Tick(2);

        Assert.Contains("Speaker:play SoundBlockAlert2", ship.Rig.Log);
    }

    [Fact]
    public void TheStepReadsWithTheSoundsName()
    {
        var ship = new Rigged();
        var speaker = ship.Speaker(1, "Speaker");
        var play = ship.Entity(speaker.Id).Kind.FindAction(SoundExtension.Play, Form.Native)!;
        var fields = new List<Field>();
        play.Fields(fields);

        // The rig's names are its ids; the game's are the terminal's, by the same field.
        fields[0] = Field.Among("sound", ["Alert 1", "Alert 2"], ["SoundBlockAlert1", "SoundBlockAlert2"]);

        Assert.Equal("Alert 2", SentenceText.Given(new ValuesSlot([Literal.Of("SoundBlockAlert2")], fields, 0)));
    }
}
