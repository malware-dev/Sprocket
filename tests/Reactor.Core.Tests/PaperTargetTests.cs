using Reactor.Editing;
using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.Ui;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// Core can be rendered by something that is not this game.
/// </summary>
/// <remarks>
/// The claim core is built on is that it states capabilities and decides nothing about
/// presentation. Only a second implementation can keep that claim honest, and the second
/// implementation has to be one that cannot quietly agree with the first - so it is text, in the
/// test project, with no drawing, no layout and no game anywhere near it.
/// <para>
/// The pressure is in <see cref="PaperTarget" /> refusing to render a component it has
/// not been told about. These tests walk the editor through every component type there is, so a new
/// one arrives here before it arrives in front of anybody.
/// </para>
/// </remarks>
public class PaperTargetTests
{
    [Fact]
    public void EveryComponentCoreProducesCanBeWrittenDown()
    {
        var ship = Build();
        var editor = ship.Editor;
        var seen = new HashSet<string>();

        Note(seen, ship);

        editor.NewSystem("Airlock");
        Note(seen, ship);

        editor.PressStart("When…");
        Note(seen, ship);

        editor.PressBlock("Airlock Button");
        Note(seen, ship);

        editor.PressChoice("is pressed");
        editor.PressPrimary();
        Note(seen, ship);

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();
        Note(seen, ship);

        editor.GoToRoot();
        Note(seen, ship);

        editor.PressStart("Load system");
        Note(seen, ship);

        Assert.Contains(nameof(StartComponent), seen);
        Assert.Contains(nameof(StatusListComponent), seen);
        Assert.Contains(nameof(PickComponent), seen);
        Assert.Contains(nameof(ChoiceListComponent), seen);
        Assert.Contains(nameof(SequenceComponent), seen);
    }

    /// <summary>The sentence survives the trip to paper, because the sentence is the product.</summary>
    [Fact]
    public void ASequenceReadsAsASentenceOnPaper()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.PressChoice("close");
        editor.PressPrimary();

        var paper = PaperTarget.Draw(ship.World, editor.Screen);

        Assert.Contains("When Airlock Button is pressed", paper);
        Assert.Contains("1. close Inner Door", paper);
    }

    /// <summary>An unfinished step says so rather than saying nothing.</summary>
    [Fact]
    public void AnEmptySlotIsDrawnAsAGap()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");

        var paper = PaperTarget.Draw(ship.World, editor.Screen);

        Assert.Contains("pick:", paper);
        Assert.Contains("[ ] Airlock Button", paper);
    }

    static void Note(HashSet<string> seen, Ship ship)
    {
        // Drawn every time, not only at the end: the refusal is what is being exercised, and a walk
        // that renders once would step straight past whatever it could not have written down.
        Assert.NotEqual("", PaperTarget.Draw(ship.World, ship.Editor.Screen));

        foreach (var component in ship.Editor.Screen.Components)
            seen.Add(component.GetType().Name);
    }

    static Ship Build()
    {
        var ship = new Ship();
        ship.World.Add(new TestDoor(1, "Inner Door"));
        ship.World.Add(new TestDoor(2, "Outer Door"));
        ship.World.Add(new TestButton(3, "Airlock Button"));
        ship.World.Add(new TestLamp(4, "Airlock Light"));

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new ReactorEditor(ship.World, ship.Automation, ship.Runner);
        return ship;
    }

    sealed class Ship
    {
        public TestWorld World = new();
        public Automation Automation = new();
        public AutomationRunner Runner = null!;
        public ReactorEditor Editor = null!;
    }
}
