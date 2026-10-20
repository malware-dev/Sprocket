using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The home screen is searched by the names of its logic systems and installations.</summary>
public class HomeSearchTests
{
    /// <summary>Two logic systems, Airlock with its first installation and one called Port, and Hangar with its first, on the home screen.</summary>
    static Simulation TwoSystems()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewSystem("Airlock");
        rig.Tick();
        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.NewSystem("Hangar");
        rig.Tick();
        rig.Editor.GoToRoot();
        rig.Tick();
        Assert.IsType<HomePage>(rig.Editor.Current);
        rig.Editor.Program.Systems[0].AddInstance().Name = "Port";
        rig.Editor.Program.Edited();
        rig.Tick();
        rig.Editor.GoToRoot();
        rig.Tick();
        return rig;
    }

    /// <summary>The names the home screen shows: each logic system shown, then each of its installations shown.</summary>
    static string[] Shown(Simulation rig)
    {
        var root = (HomePage)rig.Editor.Current!;
        return root.View!.Within().OfType<SystemGroup>().Where(group => group.IsVisible)
            .SelectMany(group => new[] { group.Heading.Summary }.Concat(group.Within().OfType<StatusRow>()
                .Where(row => row != group.Heading && row.IsVisible).Select(row => row.Summary)))
            .ToArray();
    }

    /// <summary>A logic system whose name matches shows with every installation; one that does not is hidden.</summary>
    [Fact]
    public void ALogicSystemsNameShowsItWithItsInstallations()
    {
        var rig = TwoSystems();

        ((HomePage)rig.Editor.Current!).Search("hang");
        rig.Tick();

        Assert.Equal(new[] { "Hangar", "Installation 1" }, Shown(rig));
    }

    /// <summary>An installation whose name matches shows under its logic system's heading, its other installations hidden.</summary>
    [Fact]
    public void AnInstallationsNameShowsItUnderItsLogicSystem()
    {
        var rig = TwoSystems();

        ((HomePage)rig.Editor.Current!).Search("port");
        rig.Tick();

        Assert.Equal(new[] { "Airlock", "Port" }, Shown(rig));
    }

    /// <summary>Only the first heading over installations shown links to the codex, as a page links a term once; searching the first away moves the link.</summary>
    [Fact]
    public void OnlyTheFirstInstallationsHeadingIsALink()
    {
        var rig = TwoSystems();
        var groups = ((HomePage)rig.Editor.Current!).View!.Within().OfType<SystemGroup>().ToArray();
        Assert.Equal(new[] { true, false }, groups.Select(group => group.LinksTerm));

        ((HomePage)rig.Editor.Current!).Search("hang");
        rig.Tick();

        Assert.True(groups[1].LinksTerm, "the heading shown first does not link");
    }

    /// <summary>The words searched for are kept while a screen opened from the home screen is open, and shown again on coming back.</summary>
    [Fact]
    public void TheSearchIsKeptOnComingBack()
    {
        var rig = TwoSystems();
        ((HomePage)rig.Editor.Current!).Search("hang");
        rig.Tick();

        rig.Editor.OpenLibrary();
        rig.Tick();
        rig.Editor.Back();
        rig.Tick();

        Assert.Equal("hang", ((HomePage)rig.Editor.Current!).SearchBox.Text);
        Assert.Equal(new[] { "Hangar", "Installation 1" }, Shown(rig));
    }
}
