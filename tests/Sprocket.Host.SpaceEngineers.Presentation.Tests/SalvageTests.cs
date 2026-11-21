using Sprocket.Host.SpaceEngineers.Features.Developing;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
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
    const string Older = "sprocket 1 alpha 6\n"
        + "block|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:87472652339029116,t:Button Panel\n"
        + "system|1|on|Test|\n"
        + "one|1|\n"
        + "sequence|1|on||\n"
        + "when|any|pressed_1|entity1\n";

    /// <summary>A program of this version with a step no sequence can hold.</summary>
    const string Broken = "sprocket 3 alpha 1\n\nsystem \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock {\n"
        + "    sequence 1 {\n        nonsense\n    }\n}\n";

    /// <summary>The same, mended.</summary>
    const string Mended = "sprocket 3 alpha 1\n\nsystem \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock {\n"
        + "    sequence 1 {\n        wait 60t\n    }\n}\n";

    /// <summary>The notice on the screen, or null. The driving helper throws where there is none.</summary>
    static Notice Said(Simulation rig) =>
        rig.Editor.Notice;

    /// <summary>Answers the question the first edit raises, the way a player does.</summary>
    static void Answer(Simulation rig, string text) =>
        rig.Editor.Screen.Answers.First(a => a.Text == text).Command!.Execute();

    static Store Holding(string program)
    {
        Store store = Store.Nowhere();
        store.SetValue(BlockStorage.ProgramKey, program);
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

        Assert.Equal(Older, store.GetValue(BlockStorage.ProgramKey));
    }

    /// <summary>
    /// The screen says so, where somebody will see it.
    /// </summary>
    /// <remarks>
    /// This is the whole of what was missing. The protection was working; the telling was a line in the
    /// game's log, which is the one place a player never looks.
    /// </remarks>
    [Fact]
    public void TheScreenSaysTheProgramIsFromAnotherVersion()
    {
        Simulation rig = Simulation.On(Holding(Older));
        rig.Tick();

        Notice said = Said(rig);

        Assert.NotNull(said);
        Assert.Equal(NoticeLevel.Problem, said.Level);
        Assert.Contains("different version", said.Text);

        // One line at a glance; why and what to do about it is what opening it gives.
        Assert.NotNull(said.Expands);
        Assert.Contains("gone for good", said.Detail);

        // And the reader's own words, which are what gets read back to whoever mends Sprocket.
        Assert.Contains("written for sprocket 1 alpha 6", said.Diagnostic);
    }

    /// <summary>And a program of this version with an error in its text says that instead, and offers the text.</summary>
    /// <remarks>A different fault with a different way out: nothing of it runs, and the text is where it is mended.</remarks>
    [Fact]
    public void TheScreenSaysAProgramHasAnError()
    {
        Simulation rig = Simulation.On(Holding(Broken));
        rig.Tick();

        Notice said = Said(rig);

        Assert.NotNull(said);
        Assert.Contains("with an error in it", said.Text);
        Assert.Contains("Edit the text to fix it", said.Detail);
        Assert.Contains("Line 5", said.Diagnostic);
        Assert.Equal("Edit the text", said.OfferText);
        Assert.Empty(rig.Editor.Program.Systems);
    }

    /// <summary>The banner opens the text that would not read, with what is wrong with it.</summary>
    [Fact]
    public void TheBannerOpensTheTextToMendIt()
    {
        Simulation rig = Simulation.On(Holding(Broken));
        rig.Tick();

        Said(rig).Offers!.Execute();
        rig.Tick();

        var page = Assert.IsType<ScriptPage>(rig.Editor.Current);
        _ = page.View;
        Assert.Equal(Broken, page.Listing.Text);
        Assert.StartsWith("Line 5: ", page.Problem);
        Assert.Equal("        nonsense", page.Listing.SelectedText);
    }

    /// <summary>Mending the text puts its program in place, which runs and saves like any other.</summary>
    [Fact]
    public void MendingTheTextPutsItsProgramInPlace()
    {
        Store store = Holding(Broken);
        Simulation rig = Simulation.On(store);
        rig.Tick();

        Said(rig).Offers!.Execute();
        rig.Tick();
        rig.Editor.Scripted(Mended);
        rig.Editor.ApplyScript();
        rig.Tick();

        Assert.True(rig.Editor.Program.Intact);
        Assert.Equal("Airlock", Assert.Single(rig.Editor.Program.Systems).Name);
        Assert.Equal("", Assert.IsType<ScriptPage>(rig.Editor.Current).Problem);
        var shown = Said(rig);
        Assert.True(shown == null || shown.Leaving, "the banner stayed");

        rig.Save();
        Assert.Contains("as Airlock", store.GetValue(BlockStorage.ProgramKey));
    }

    /// <summary>A text still wrong changes nothing, and says what is wrong with it now.</summary>
    [Fact]
    public void ATextStillWrongChangesNothing()
    {
        Store store = Holding(Broken);
        Simulation rig = Simulation.On(store);
        rig.Tick();

        Said(rig).Offers!.Execute();
        rig.Tick();
        rig.Editor.Scripted(Mended.Replace("sequence 1 {", "sequence {"));
        rig.Editor.ApplyScript();
        rig.Tick();

        Assert.False(rig.Editor.Program.Intact);
        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Contains("expected the sequence's number", Assert.IsType<ScriptPage>(rig.Editor.Current).Problem);

        rig.Save();
        Assert.Equal(Broken, store.GetValue(BlockStorage.ProgramKey));
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

        // The first edit asks before it replaces anything, so this is a player pressing through it,
        // then naming the logic system it made.
        rig.Editor.PressStart("logic system");
        Answer(rig, "Replace it");
        rig.Editor.Screen.Heading[0].Renames!("Airlock");
        rig.Tick();
        rig.Save();

        var saved = store.GetValue(BlockStorage.ProgramKey);

        Assert.StartsWith("sprocket 3 alpha 1", saved);
        Assert.Contains("Airlock", saved);
        Assert.DoesNotContain("|", saved);

        // And it keeps saving, because the block no longer holds anything unreadable. Back to the
        // root first: making a system walks into it, so the way to add another is from the front.
        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.NewSystem("Hangar");
        rig.Tick();
        rig.Save();

        Assert.Contains("Hangar", store.GetValue(BlockStorage.ProgramKey));
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

        Assert.Equal(Older, store.GetValue(BlockStorage.ProgramKey));
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
        Assert.StartsWith("sprocket 3 alpha 1", store.GetValue(BlockStorage.ProgramKey));
        Assert.Null(Said(rig));
    }

    /// <summary>And saying no from the banner leaves everything as it was.</summary>
    [Fact]
    public void TheBannerLeavesItAloneWhenToldTo()
    {
        Store store = Holding(Older);

        Simulation rig = Simulation.On(store);
        rig.Tick();

        Said(rig).Offers.Execute();
        rig.Tick();
        Answer(rig, "Leave it alone");
        rig.Tick();
        rig.Save();

        Assert.False(rig.Editor.Screen.HasQuestion);
        Assert.False(Said(rig).Leaving);
        Assert.Equal(Older, store.GetValue(BlockStorage.ProgramKey));
    }

    /// <summary>
    /// The banner carries the mending, so it can be dealt with where it is read.
    /// </summary>
    /// <remarks>
    /// It asks first, the way the first edit does, because it throws away the same program.
    /// Afterwards the block saves like any other, and the banner has nothing left to say.
    /// </remarks>
    [Fact]
    public void TheBannerCanResolveItWhereItStands()
    {
        Store store = Holding(Older);

        Simulation rig = Simulation.On(store);
        rig.Tick();

        Notice said = Said(rig);

        Assert.Equal("Replace it", said.OfferText);
        Assert.NotNull(said.Offers);

        said.Offers.Execute();
        rig.Tick();

        Assert.True(rig.Editor.Screen.HasQuestion, "the banner replaced the program without asking");
        Assert.False(Said(rig).Leaving);

        Answer(rig, "Replace it");
        rig.Tick();

        // Resolved: the banner is going, and the block takes what is here now.
        Assert.False(rig.Editor.Screen.HasQuestion);
        Assert.True(Said(rig).Leaving);

        rig.Editor.NewSystem("Airlock");
        rig.Tick();
        rig.Save();

        Assert.StartsWith("sprocket 3 alpha 1", store.GetValue(BlockStorage.ProgramKey));
        Assert.Contains("Airlock", store.GetValue(BlockStorage.ProgramKey));
    }
}
