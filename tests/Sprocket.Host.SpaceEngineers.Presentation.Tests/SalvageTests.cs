using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block holding a program this build cannot read.
/// </summary>
/// <remarks>
/// The failure these exist for cost a session's work. A world carried a program in the format from
/// before the script language; loading it failed, which marked the program not to be written back;
/// and nothing said so anywhere but the game's log. So the editor showed an empty construct, building
/// in it felt like building in any other, and none of it was ever written to the block.
/// <para>
/// Two rules come out of that, and they are in tension only until you say them out loud. A failed load
/// is failed, so merely looking at the block writes nothing over what it holds - somebody can still
/// decide what to do about it. And the moment they edit, what they see is what they meant to have, so
/// it saves like any other program.
/// </para>
/// </remarks>
public class SalvageTests
{
    /// <summary>What the old format looked like, which is what his test world still held.</summary>
    const string Older = "sprocket 1 alpha 5\n"
        + "block|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:87472652339029116,t:Button Panel\n"
        + "system|1|on|Test|\n"
        + "one|1|\n"
        + "sequence|1|on||\n"
        + "when|any|pressed_1|entity1\n";

    /// <summary>The notice on the screen, or null. The driving helper throws where there is none.</summary>
    static NoticeComponent Said(Simulation rig) =>
        rig.Editor.Screen.Components.OfType<NoticeComponent>().FirstOrDefault();

    /// <summary>Answers the question the first edit raises, the way a player does.</summary>
    static void Answer(Simulation rig, string text) =>
        rig.Editor.Invoke(rig.Editor.Screen.Answers.First(a => a.Text == text).Command);

    static Store Holding(string program)
    {
        Store store = Store.Nowhere();
        store.SetValue(SprocketUnit.ProgramKey, program);
        return store;
    }

    /// <summary>
    /// Looking at it changes nothing on the block.
    /// </summary>
    /// <remarks>
    /// Opening the editor, ticking, and closing it again is what somebody does before they have decided
    /// anything. The program that would not read is still there afterwards, so whatever they decide is
    /// still open to them.
    /// </remarks>
    [Fact]
    public void LookingAtAProgramThatWouldNotReadLeavesItAlone()
    {
        Store store = Holding(Older);

        Simulation rig = Simulation.On(store);
        rig.Tick();
        rig.Save();

        Assert.Equal(Older, store.GetValue(SprocketUnit.ProgramKey));
    }

    /// <summary>
    /// The screen says so, where somebody will see it.
    /// </summary>
    /// <remarks>
    /// This is the whole of what was missing. The protection was working; the telling was a line in the
    /// game's log, which is the one place a player never looks.
    /// </remarks>
    [Fact]
    public void TheScreenSaysTheProgramCouldNotBeRead()
    {
        Simulation rig = Simulation.On(Holding(Older));
        rig.Tick();

        NoticeComponent said = Said(rig);

        Assert.NotNull(said);
        Assert.Equal(NoticeLevel.Problem, said.Level);
        Assert.Contains("cannot read", said.Text);

        // One line at a glance; why and what to do about it is what opening it gives.
        Assert.NotEqual(0, said.ExpandCommand);
        Assert.Contains("replaces it for good", said.Detail);

        // And the reader's own words, which are what gets read back to whoever mends Sprocket.
        Assert.Contains("means nothing here", said.Diagnostic);
    }

    /// <summary>
    /// Editing it makes what is on the screen the truth, and it saves.
    /// </summary>
    /// <remarks>
    /// The rule that matters most: a person editing must be able to trust that what they see is what
    /// they have. Before this, every rule built on such a block was kept in memory and lost with the
    /// process, silently.
    /// </remarks>
    [Fact]
    public void EditingItMakesItTheTruthAndSaves()
    {
        Store store = Holding(Older);

        Simulation rig = Simulation.On(store);

        // The first edit asks before it replaces anything, so this is a player pressing through it.
        rig.Editor.NewSystem("Airlock");
        Answer(rig, "Replace it");
        rig.Tick();
        rig.Save();

        var saved = store.GetValue(SprocketUnit.ProgramKey);

        Assert.StartsWith("sprocket 1 alpha 5", saved);
        Assert.Contains("Airlock", saved);
        Assert.DoesNotContain("|", saved);

        // And it keeps saving, because the block no longer holds anything unreadable. Back to the
        // root first: making a system walks into it, so the way to add another is from the front.
        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.NewSystem("Hangar");
        rig.Tick();
        rig.Save();

        Assert.Contains("Hangar", store.GetValue(SprocketUnit.ProgramKey));
    }

    /// <summary>
    /// Answering the question resolves it, and the banner goes with it.
    /// </summary>
    /// <remarks>
    /// The question is the decision, so it is the moment the warning stops being true: from then on
    /// the block is going to hold what is on the screen. Waiting for the save would leave a banner up
    /// describing something the person has already dealt with.
    /// <para>
    /// It is told to leave rather than dropped, and stays in the list until whatever is drawing it
    /// says it has finished going - a banner taken away between two frames is the jump the whole
    /// arrangement avoids.
    /// </para>
    /// </remarks>
    [Fact]
    public void AnsweringTheQuestionTakesTheBannerWithIt()
    {
        Simulation rig = Simulation.On(Holding(Older));

        rig.Tick();
        Assert.Equal(NoticeLevel.Problem, Said(rig).Level);
        Assert.False(Said(rig).Leaving);

        rig.Editor.NewSystem("Airlock");
        Answer(rig, "Replace it");
        rig.Tick();

        Assert.True(Said(rig).Leaving);
    }

    /// <summary>
    /// The first edit asks first, and leaving it alone leaves the block exactly as it was.
    /// </summary>
    /// <remarks>
    /// The point of asking at all. A failed load is recoverable right up until the edit that replaces
    /// it, so the press that crosses that line is the one that has to be deliberate - and somebody who
    /// says no must be no worse off than before they pressed.
    /// </remarks>
    [Fact]
    public void TheFirstEditAsksAndLeavingItAloneChangesNothing()
    {
        Store store = Holding(Older);

        Simulation rig = Simulation.On(store);
        rig.Editor.NewSystem("Airlock");

        Assert.True(rig.Editor.Screen.HasQuestion);
        Assert.Contains("Replace the program", rig.Editor.Screen.Question);

        Answer(rig, "Leave it alone");
        rig.Tick();
        rig.Save();

        Assert.Equal(Older, store.GetValue(SprocketUnit.ProgramKey));
        Assert.Empty(rig.Editor.Program.Systems);
    }

    /// <summary>A program this build can read is saved without any of the above.</summary>
    [Fact]
    public void AProgramThatReadsIsSavedAsEver()
    {
        Store store = Store.Nowhere();

        Simulation rig = Simulation.On(store);
        rig.Editor.NewSystem("Airlock");
        rig.Tick();
        rig.Save();

        Assert.False(rig.Editor.Screen.HasQuestion);
        Assert.StartsWith("sprocket 1 alpha 5", store.GetValue(SprocketUnit.ProgramKey));
        Assert.Null(Said(rig));
    }

    /// <summary>
    /// The banner carries the mending, so it can be dealt with where it is read.
    /// </summary>
    /// <remarks>
    /// The same decision the question asks for, a step earlier: somebody who has opened the banner and
    /// read what it costs presses it there rather than hunting for an edit to make. Afterwards the
    /// block saves like any other, and the banner has nothing left to say.
    /// </remarks>
    [Fact]
    public void TheBannerCanResolveItWhereItStands()
    {
        Store store = Holding(Older);

        Simulation rig = Simulation.On(store);
        rig.Tick();

        NoticeComponent said = Said(rig);
        ChoiceEntry replace = said.Actions.Single();

        Assert.Equal("Replace it", replace.Text);
        Assert.NotEqual(0, replace.Command);

        rig.Editor.Invoke(replace.Command);
        rig.Tick();

        // Resolved: no question was needed, the banner is going, and the block takes what is here now.
        Assert.False(rig.Editor.Screen.HasQuestion);
        Assert.True(Said(rig).Leaving);

        rig.Editor.NewSystem("Airlock");
        rig.Tick();
        rig.Save();

        Assert.StartsWith("sprocket 1 alpha 5", store.GetValue(SprocketUnit.ProgramKey));
        Assert.Contains("Airlock", store.GetValue(SprocketUnit.ProgramKey));
    }
}
