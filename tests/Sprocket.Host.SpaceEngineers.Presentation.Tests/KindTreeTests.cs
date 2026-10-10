using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Sim.Machine;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The list of kinds is a tree to the eye: kinds of block that are one thing to a player stand under a family, such as a
/// sliding door under Door, and choosing the family takes every block in it. A family with one kind on the construct is
/// that kind alone.
/// </summary>
public class KindTreeTests
{
    /// <summary>A sliding door is its own type in the game, and still hangs under Door with the ordinary doors.</summary>
    [Fact]
    public void ASlidingDoorHangsUnderDoor()
    {
        var screen = OnATask();

        var kinds = Blocks(screen).Kinds();
        var family = kinds.FindIndex(k => k.Text == "Door" && !k.IsNested);
        Assert.True(family >= 0, "no Door family");
        Assert.Equal(3, kinds[family].Count);
        Assert.Equal(new[] { "Door", "Sliding Door" }, kinds.Skip(family + 1).TakeWhile(k => k.IsNested).Select(k => k.Text));
    }

    /// <summary>Choosing a family lists every block of every kind in it, and no other.</summary>
    [Fact]
    public void ChoosingAFamilyTakesEveryBlockInIt()
    {
        var screen = OnATask();

        Blocks(screen).Kinds().First(k => k.Text == "Door" && !k.IsNested).Command!.Execute();
        screen.Run(2);

        Assert.Equal(new[] { "Airlock Inner Door", "Airlock Outer Door", "Bay Slider" },
            Blocks(screen).Entries().Select(e => e.Text).OrderBy(t => t));
        Assert.Equal("Door", Blocks(screen).Kind);
    }

    /// <summary>A kind found by the search over the kinds comes with its family above it, and its family's other kinds stay out.</summary>
    [Fact]
    public void AKindFoundBySearchKeepsItsFamily()
    {
        var screen = OnATask();

        Blocks(screen).SearchKinds("sliding");
        screen.Run(2);

        var kinds = Blocks(screen).Kinds().Skip(1).Select(k => (k.Text, k.IsNested));
        Assert.Equal(new[] { ("Door", false), ("Sliding Door", true) }, kinds);
    }

    /// <summary>A family with a single kind on the construct is not listed; the kind stands on its own.</summary>
    [Fact]
    public void AFamilyWithOneKindStandsAsThatKind()
    {
        var screen = OnATask();

        var kinds = Blocks(screen).Kinds();
        Assert.DoesNotContain(kinds, k => k.Text == "Hangar door");
        Assert.Contains(kinds, k => k.Text == "Hangar Door" && !k.IsNested);
    }

    /// <summary>Opening the list of kinds gives the keyboard to the box that narrows it.</summary>
    [Fact]
    public void OpeningTheKindsFocusesTheirSearch()
    {
        var screen = OnATask();

        screen.Do("press", "Anything");

        Assert.Same(Blocks(screen).KindNarrow, screen.Editor.Input.Focused);
    }

    /// <summary>More kinds than the screen has room for scroll inside the list, which stays on the screen.</summary>
    [Fact]
    public void ALongListOfKindsStaysOnTheScreen()
    {
        var rig = Rig.Demo();
        for (var i = 0; i < 40; i++)
            rig.Weld(new Plain(500 + i, "Lamp " + i, new MyObjectBuilderType(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_InteriorLight)),
                "Lamp" + i, "Lamp Model " + i));
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Run(2);

        screen.Do("press", "Anything");

        Lumel.Element list = Blocks(screen).Kinds()[0];
        while (list is not Lumel.Viewing.ScrollView)
            list = list.Parent;
        Assert.True(list.Bounds.Y >= 0f && list.Bounds.Y + list.Bounds.Height <= 1080f, "the list runs off the screen at " + list.Bounds);
        Assert.True(list.Bounds.Height < Blocks(screen).Kinds().Sum(k => k.Bounds.Height), "the list does not scroll");
    }

    /// <summary>The demo construct with a sliding door added, on a new step's picker.</summary>
    static Screen OnATask()
    {
        var rig = Rig.Demo();
        rig.Weld(new Door(40, "Bay Slider", new MyObjectBuilderType(typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_AirtightSlideDoor)),
            "LargeBlockSlideDoor", "Sliding Door"));
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Run(2);
        return screen;
    }

    static StageColumn Blocks(Screen screen) => screen.Editor.Pick().Stages[0];
}
