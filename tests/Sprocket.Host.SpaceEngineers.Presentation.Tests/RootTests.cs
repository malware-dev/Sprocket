using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The root, drawn by its own view: rows kept by the system they show, and presses that go the editor's way.</summary>
public class RootTests
{
    /// <summary>Another logic system adds one group, and the group already there is the same group.</summary>
    [Fact]
    public void ANewSystemKeepsTheGroupsAlreadyShown()
    {
        var sim = Simulation.Fresh();
        Built(sim, "Airlock");
        var airlock = sim.Editor.RootHeadings().Single();

        Repeating(sim, "Hangar");
        var headings = sim.Editor.RootHeadings();

        Assert.Equal(2, headings.Count);
        Assert.Same(airlock, headings[0]);
        Assert.Equal(new[] { "Airlock", "Hangar" }, sim.Editor.RootHeadingNames());
    }

    /// <summary>One list, in the program's order: each logic system its heading, its installation under it, and a way to add another.</summary>
    [Fact]
    public void EachLogicSystemStandsWithItsInstallations()
    {
        var sim = Simulation.Fresh();
        Built(sim, "Airlock");
        Repeating(sim, "Hangar");

        Assert.Equal(new[] { "Airlock", "Hangar" }, sim.Editor.RootHeadingNames());
        Assert.Equal(new[] { "Installation 1", "Installation 1" }, sim.Editor.RootRows().Select(row => row.Summary));
        Assert.Equal(2, sim.Editor.RootAdds().Count);
    }

    /// <summary>
    /// Every installation's switch is for its one installation, and its note says so in the words for one thing rather than the
    /// words for a system over everything in it.
    /// </summary>
    [Fact]
    public void EachSwitchIsForItsOneInstallation()
    {
        var sim = Simulation.Fresh();
        Built(sim, "Airlock");
        Repeating(sim, "Hangar");

        // On, so each installation shows its switch as it stands: a logic system switched off shows every one of it off.
        foreach (var system in sim.Editor.Program.Systems)
            system.Enabled = true;
        sim.Tick();

        var rows = sim.Editor.RootRows();
        Assert.Equal(sim.Texts.RowSwitchOff, rows[0].SwitchPart.Tip?.Text);
        Assert.Equal(sim.Texts.RowSwitchOff, rows[1].SwitchPart.Tip?.Text);
    }

    /// <summary>While a question stands, a press on the root puts it away and does nothing else.</summary>
    [Fact]
    public void APressWhileAQuestionStandsOnlyPutsItAway()
    {
        var store = Store.Nowhere();
        store.SetValue(SprocketUnit.ProgramKey, Older);
        var sim = Simulation.On(store);
        sim.Tick();

        sim.Editor.PressStart("logic system");
        Assert.True(sim.Editor.Screen.HasQuestion, "replacing an unreadable program asked nothing");

        sim.Editor.PressStart("logic system");

        Assert.False(sim.Editor.Screen.HasQuestion);
        Assert.IsType<HomePage>(sim.Editor.Current);
        Assert.Empty(sim.Editor.Program.Systems);
    }

    /// <summary>
    /// A question the editor asks as the dialog stands in the middle of the screen, rather than hanging
    /// under the press that raised it.
    /// </summary>
    [Fact]
    public void AQuestionFromTheRootIsTheDialog()
    {
        var screen = Screen.Holding(Older, "tick", "2");

        screen.Editor.PressStart("logic system");
        screen.Run(2);

        var keep = screen.Text(screen.Editor.Texts.SalvageKeep);
        Assert.InRange(keep.At.Y, 1080f / 3f, 1080f * 2f / 3f);
    }

    /// <summary>A Sprocket somebody else holds can be looked round from its root, and not changed there.</summary>
    [Fact]
    public void WhileSomebodyElseHoldsItTheRootOffersNoEdits()
    {
        var sim = Simulation.Fresh();
        Built(sim, "Airlock");

        var store = Store.Nowhere();
        sim.Unit.Written(out var program, out _);
        store.SetValue(SprocketUnit.ProgramKey, program);
        var lent = Simulation.On(store);
        lent.Editor.Holder = "Bert";
        lent.Tick();

        var row = lent.Editor.RootRows().Single();
        Assert.NotNull(row.Opens);
        Assert.Null(row.Offers);
        Assert.Null(row.Flips);
    }

    /// <summary>The root speaks the language loaded, from its buttons down.</summary>
    [Fact]
    public void TheRootSpeaksTheLanguageLoaded()
    {
        var sim = Simulation.Fresh();
        sim.Editor.RootStarts();

        sim.Texts.Book.Load(new ResxTexts(ResxTexts.Beside, "no"));
        sim.Editor.Invalidate();
        sim.Tick();

        Assert.Equal(new[] { "+ logikksystem" }, sim.Editor.RootStarts());
    }

    const string Older = "sprocket 1 alpha 6\n"
        + "block|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:87472652339029116,t:Button Panel\n"
        + "system|1|on|Test|\n"
        + "one|1|\n"
        + "sequence|1|on||\n"
        + "when|any|pressed_1|entity1\n";

    /// <summary>A logic system named <paramref name="name" />, started by the airlock's button, and the root shown again.</summary>
    static void Built(Simulation sim, string name)
    {
        sim.Editor.NewButtonSystem(name);
        sim.Editor.GoToRoot();
        sim.Tick();
    }

    /// <summary>A logic system named <paramref name="name" /> that closes the outer door on a timer, and the root shown again.</summary>
    static void Repeating(Simulation sim, string name)
    {
        sim.Editor.NewSystem(name);
        sim.Editor.PressStart("every");
        sim.Editor.DeveloperMode = true;
        sim.Tick();
        sim.Editor.PressChoice("A task on blocks…");
        sim.Editor.PressBlock("Airlock Outer Door");
        sim.Editor.PressChoice("Close");
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        sim.Editor.GoToRoot();
        sim.Tick();
    }
}
