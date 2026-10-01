using System.Globalization;
using Sprocket.Host.SpaceEngineers.Features.Developing;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The listing of what a construct's automation is saved as, and the way in to it.
/// </summary>
/// <remarks>
/// A developer tool. The thing worth testing is that it is not there for anybody else: a way in
/// offered by default is a way in somebody who came here to play will press.
/// </remarks>
public class ScriptPageTests
{
    const string Called = "Script";

    static readonly string Way = Called.ToUpperInvariant();

    const string Back = Symbols.Left;

    /// <summary>
    /// A screen told what it is for, and told nothing else.
    /// </summary>
    /// <remarks>
    /// <b>Saying so is all a host does, and this may do no more than a host does.</b> In the game the
    /// flag is set on a unit that has just been built, which is after the editor's constructor has
    /// already built its first screen - so a test that rebuilds the screen itself would pass on a
    /// build where the way in never appears.
    /// </remarks>
    static Screen Developing()
    {
        var screen = Screen.Fresh();
        screen.Editor.DeveloperMode = true;

        return screen.Run(1);
    }

    static Screen Press(Screen screen, string label)
    {
        var at = screen.Text(label);
        var point = Screen.Ui(at.At.X + 4f, at.At.Y + 4f);

        return screen.Do(
            point.X.ToString("0.##", CultureInfo.InvariantCulture),
            point.Y.ToString("0.##", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TheWayInIsNotOfferedUnlessItWasAskedFor()
    {
        Assert.False(Screen.Fresh().Shows(Way));
    }

    [Fact]
    public void DeveloperModeOffersTheWayIn()
    {
        Assert.True(Developing().Shows(Way));
    }

    /// <summary>
    /// It is on the screen rather than on what the screen is about.
    /// </summary>
    /// <remarks>
    /// Looking at the script is not a thing you add to a construct, so it does not belong among the
    /// ways to add one - and it is worth reaching from wherever somebody happens to be standing.
    /// </remarks>
    [Fact]
    public void TheWayInIsOnEveryScreen()
    {
        Screen screen = Developing();

        Assert.Contains(screen.Editor.Screen.Actions, entry => entry.Text == Called);
        Assert.DoesNotContain(screen.Words, said => said.Contains("+ " + Called));
    }

    /// <summary>The listing shows the script, beginning where every saved program begins.</summary>
    [Fact]
    public void ItShowsWhatTheProgramIsSavedAs()
    {
        Screen screen = Press(Developing(), Way);

        Assert.Equal(Called, screen.Ended);
        Assert.Contains(screen.Legible, said => said.StartsWith("sprocket "));
    }

    /// <summary>
    /// Pressing it while looking at it is not a second one on top of the first.
    /// </summary>
    /// <remarks>
    /// Proved by walking back rather than by counting: one step back from a listing opened twice has
    /// to leave the listing behind entirely, and would not if the second press had stacked one.
    /// </remarks>
    [Fact]
    public void PressingItWhileOnItStaysWhereItIs()
    {
        Screen screen = Developing();
        string home = screen.Ended;

        Press(Press(screen, Way), Way);
        Assert.Equal(Called, screen.Ended);

        Press(screen, Back);
        Assert.Equal(home, screen.Ended);
    }

    /// <summary>
    /// The listing claims the body and does its own scrolling.
    /// </summary>
    /// <remarks>
    /// A page that does not claim the height is wrapped in the screen's own scroller, which puts a second bar around the
    /// listing's and lets the two fight over one gesture, and draws a thumb on a page that has nothing to scroll.
    /// </remarks>
    [Fact]
    public void TheListingIsTheWholeBodyAndScrollsItself()
    {
        Screen screen = Press(Developing(), Way);
        screen.Run(1);

        var page = Assert.IsType<ScriptPage>(screen.Editor.Current);
        Assert.True(page.ScrollsItself, "the listing does not scroll itself");

        // Nothing to scroll on a program this short, so no thumb belongs anywhere on the screen.
        Assert.DoesNotContain(screen.Drawings, d => !d.IsText && d.Ink == Palette.Thumb);
    }

    /// <summary>The script edited by hand and applied is the program, and what was switched off stays off.</summary>
    [Fact]
    public void AnEditedScriptIsTheProgram()
    {
        Screen screen = Developing();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.GoToRoot();
        screen.Run(1);

        var system = screen.Editor.Program.Systems[0];
        var identity = system.Identity;
        Assert.False(system.Enabled);

        Press(screen, Way);
        screen.Run(1);

        var page = Assert.IsType<ScriptPage>(screen.Editor.Current);
        screen.Editor.Scripted(page.Listing.Text.Replace(".Airlock {", ".Hangar {"));
        Assert.True(screen.Shows("Apply"), "the script page has no Apply button");
        Press(screen, "Apply");
        screen.Run(1);

        var now = Assert.Single(screen.Editor.Program.Systems);
        Assert.Equal("Hangar", now.Name);
        Assert.Equal(identity, now.Identity);
        Assert.False(now.Enabled);
        Assert.Contains(".Hangar {", page.Listing.Text);
    }

    /// <summary>A script edited into something with an error in it changes nothing, and says what is wrong.</summary>
    [Fact]
    public void AnEditedScriptWithAnErrorChangesNothing()
    {
        Screen screen = Developing();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.GoToRoot();
        screen.Run(1);
        Press(screen, Way);
        screen.Run(1);

        var page = Assert.IsType<ScriptPage>(screen.Editor.Current);
        screen.Editor.Scripted(page.Listing.Text.Replace(".Airlock {", ".Airlock garbage {"));
        Assert.True(screen.Shows("Apply"), "the script page has no Apply button");
        Press(screen, "Apply");
        screen.Run(1);

        Assert.Equal("Airlock", Assert.Single(screen.Editor.Program.Systems).Name);
        Assert.True(screen.Editor.Program.Intact);
        Assert.StartsWith("Line ", page.Problem);
    }

    /// <summary>
    /// Nothing is measured on a tick where nobody has touched it.
    /// </summary>
    /// <remarks>
    /// The listing is written out afresh once a second and handed over whole, so the risk is a screen
    /// that re-reads a thousand lines of text every frame because the string it was given was new.
    /// </remarks>
    [Fact]
    public void AnIdleSecondOnTheListingMeasuresNoText()
    {
        Screen screen = Press(Developing(), Way);
        screen.Run(5);

        var settled = screen.Measured;
        screen.Run(60);

        Assert.Equal(settled, screen.Measured);
    }
}
